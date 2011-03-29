/**
 * Copyright (C) 2007 Google Inc.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA
 */

/*
 * Ported by Dario Quintana;
 * Rewritten by Gerke Geurts, as NHibernate transactions are not reusable 
 * after completion, whereas Hibernate transactions are.
 */

using System;
using System.Collections.Generic;
using System.Data;
using NHibernate.Shards.Engine;
using NHibernate.Transaction;

namespace NHibernate.Shards.Transaction
{
    /// <summary>
    /// NHibernate <see cref="ITransaction"/> implementation for a transaction that can span one or more
    /// shards. If a transaction spans more than one shard, it is important to use a distributed 
    /// transaction manager across all shards. Otherwise consistent committing or rolling back of 
    /// transactions cannot be guaranteed.
    /// </summary>
    /// <remarks>
    /// This implementation deviates from the Hibernate version, as NHibernate <see cref="ITransaction"/>
    /// implementations are often disposed automatically on commit or rollback and therefore do not 
    /// support beginning a new transaction after completion of a previous transaction.
    /// </remarks>
    public class ShardedTransactionImpl : IShardedTransaction
    {
        private static readonly IInternalLogger Log = LoggerProvider.LoggerFor(typeof(ShardedTransactionImpl));

        private IShardedSessionImplementor shardedSession;
        private IList<ITransaction> transactions;
        private IList<ISynchronization> synchronizations;
        private IsolationLevel currentIsolationLevel;
        private bool begun;
        private bool commitFailed;
        private bool committed;
        private bool rolledBack;
        private bool disposed;

        public ShardedTransactionImpl(IShardedSessionImplementor shardedSession)
            : this(shardedSession, IsolationLevel.Unspecified)
        { }

        public ShardedTransactionImpl(IShardedSessionImplementor shardedSession, IsolationLevel isolationLevel)
        {
            this.shardedSession = shardedSession;
            this.currentIsolationLevel = isolationLevel;
        }

        public bool IsActive
        {
            get { return begun && !(rolledBack || committed); }
        }

        public bool WasRolledBack
        {
            get { return rolledBack; }
        }

        public bool WasCommitted
        {
            get { return committed; }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                DisposeShardTransactions();
                transactions = null;

                // Assume transaction rollback
                if (this.IsActive)
                {
                    shardedSession.AfterTransactionCompletion(this, false);
                }
                shardedSession = null;
            }
        }

        public void Begin()
        {
            if (begun) return;

            CheckNotDisposed();

            if (commitFailed)
            {
                throw new TransactionException("Cannot re-start transaction after failed commit");
            }

            AfterTransactionBegin();
        }

        public void Begin(IsolationLevel isolationLevel)
        {
            this.currentIsolationLevel = isolationLevel;
            Begin();
        }

        public void Commit()
        {
            CheckNotDisposed();
            CheckBegun();

            Log.Debug("Starting transaction commit");
            NotifyLocalSynchsBeforeTransactionCompletion();

            try
            {
                CommitShardTransactions();

                AfterTransactionCompletion(true);
                committed = true;
                Dispose();
            }
            catch
            {
                try
                {
                    RollbackShardTransactions();
                }
                finally
                {
                    AfterTransactionCompletion(null);
                    commitFailed = true;
                }
                throw;
            }
        }

        public void Rollback()
        {
            // To maintain compatibility with Hibernate tests
            if (commitFailed)
            {
                rolledBack = true;
                return;
            }

            CheckNotDisposed();
            CheckBegun();

            try
            {
                RollbackShardTransactions();
                rolledBack = true;
                Dispose();
            }
            finally
            {
                AfterTransactionCompletion(false);
            }
        }

        public void Enlist(ISession session)
        {
            if (!begun) return;
            BeginShardTransaction(session);
        }

        public void Enlist(IDbCommand command)
        {
            const string MESSAGE = "Can't enlist a commmand succesfully";

            if (transactions == null) return;

            foreach (ITransaction t in transactions)
            {
                //TODO: Should I finish with the first exception?
                try
                {
                    t.Enlist(command);
                }
                catch (Exception e)
                {
                    Log.Warn(MESSAGE, e);
                    throw new TransactionException(MESSAGE, e);
                }
            }
        }

        public void RegisterSynchronization(ISynchronization sync)
        {
            if (sync == null) throw new ArgumentNullException("sync");
            if (synchronizations == null)
            {
                synchronizations = new List<ISynchronization>();
            }
            synchronizations.Add(sync);
        }

        private void BeginShardTransaction(ISession session)
        {
            if (transactions == null)
            {
                transactions = new SynchronizedCollection<ITransaction>();
            }

            try
            {
                transactions.Add(session.BeginTransaction(currentIsolationLevel));
            }
            catch (HibernateException e)
            {
                const string MESSAGE = "Cannot start underlying transaction";
                Log.Warn(MESSAGE, e);
                throw new TransactionException(MESSAGE, e);
            }
        }

        private void CommitShardTransactions()
        {
            if (transactions == null) return;

            HibernateException firstCommitException = null;

            foreach (ITransaction t in transactions)
            {
                try
                {
                    if (t.IsActive) t.Commit();
                }
                catch (HibernateException he)
                {
                    Log.Warn("Exception committing underlying transaction", he);

                    // we're only going to rethrow the first commit exception we receive
                    if (firstCommitException == null)
                    {
                        firstCommitException = he;
                    }
                }
            }

            if (firstCommitException != null)
            {
                throw new TransactionException("Commit failed", firstCommitException);
            }

            DisposeShardTransactions();
            transactions.Clear();
        }

        private void RollbackShardTransactions()
        {
            if (transactions == null) return;

            try
            {
                HibernateException firstRollbackException = null;

                foreach (ITransaction t in transactions)
                {
                    try
                    {
                        if (t.IsActive) t.Rollback();
                    }
                    catch (HibernateException he)
                    {
                        Log.Warn("Cannot rollback underlying transaction", he);

                        // we're only going to rethrow the first commit exception we receive
                        if (firstRollbackException == null)
                        {
                            firstRollbackException = he;
                        }
                    }
                }

                if (firstRollbackException != null)
                {
                    throw new TransactionException("Rollback failed", firstRollbackException);
                }
            }
            finally
            {
                DisposeShardTransactions();
                transactions.Clear();
            }
        }

        private void DisposeShardTransactions()
        {
            const string MESSAGE = "Can't dispose properly";

            if (transactions == null) return;

            Exception firstException = null;
            foreach (ITransaction t in transactions)
            {
                try
                {
                    t.Dispose();
                }
                catch (Exception e)
                {
                    Log.Warn(MESSAGE, e);
                    firstException = e;
                }
            }

            if (firstException != null)
            {
                throw new TransactionException(MESSAGE, firstException);
            }
        }

        private void AfterTransactionBegin()
        {
            begun = true;
            committed = false;
            rolledBack = false;

            if (this.shardedSession != null)
            {
                try
                {
                    this.shardedSession.AfterTransactionBegin(this);
                }
                catch (HibernateException)
                {
                    try
                    {
                        RollbackShardTransactions();
                    }
                    catch (HibernateException)
                    {
                        // What now?
                    }
                    finally
                    {
                        begun = false;
                    }
                    throw;
                }
            }
        }

        private void AfterTransactionCompletion(bool? success)
        {
            if (this.shardedSession != null)
            {
                shardedSession.AfterTransactionCompletion(this, success);
                this.shardedSession = null;
            }

            begun = false;
            NotifyLocalSynchsAfterTransactionCompletion(success);
        }

        private void NotifyLocalSynchsBeforeTransactionCompletion()
        {
            if (synchronizations != null)
            {
                for (int i = 0; i < synchronizations.Count; i++)
                {
                    ISynchronization sync = synchronizations[i];
                    try
                    {
                        sync.BeforeCompletion();
                    }
                    catch (Exception e)
                    {
                        Log.Error("exception calling user Synchronization", e);
                    }
                }
            }
        }

        private void NotifyLocalSynchsAfterTransactionCompletion(bool? success)
        {
            if (synchronizations != null)
            {
                for (int i = 0; i < synchronizations.Count; i++)
                {
                    ISynchronization sync = synchronizations[i];
                    try
                    {
                        sync.AfterCompletion(success ?? false);
                    }
                    catch (Exception e)
                    {
                        Log.Error("exception calling user Synchronization", e);
                    }
                }
            }
        }

        private void CheckNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("AdoTransaction");
            }
        }

        private void CheckBegun()
        {
            if (!begun)
            {
                throw new TransactionException("Transaction not successfully started");
            }
        }
    }
}