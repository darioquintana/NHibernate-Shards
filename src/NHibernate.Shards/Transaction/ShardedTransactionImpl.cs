using System;
using System.Collections.Generic;
using System.Data;
using log4net;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Session;
using NHibernate.Transaction;

namespace NHibernate.Shards.Transaction
{
	//TODO: some methods without implementation
	public class ShardedTransactionImpl : IShardedTransaction
	{
		private readonly ILog log = LogManager.GetLogger(typeof (ShardedTransactionImpl));
		private readonly IList<ITransaction> transactions;
		private bool begun;
		private bool commitFailed;
		private bool committed;
		private bool rolledBack;
		//private IList<Synchronization> synchronizations;
		private int timeout;
		private bool timeoutSet;
	    private IsolationLevel isoLevel = IsolationLevel.Unspecified;

		public ShardedTransactionImpl(IShardedSessionImplementor ssi)
		{
			IOpenSessionEvent osEvent = new SetupTransactionOpenSessionEvent(this);
			transactions = new SynchronizedCollection<ITransaction>();
			foreach (IShard shard in ssi.Shards)
			{
				if (shard.Session != null)
				{
					transactions.Add(shard.Session.Transaction);
				}
				else
				{
					shard.AddOpenSessionEvent(osEvent);
				}
			}
		}

        public ShardedTransactionImpl(IShardedSessionImplementor ssi, IsolationLevel isoLevel):this(ssi)
        {
            this.isoLevel = isoLevel;
        }

		#region IShardedTransaction Members

		public void SetupTransaction(ISession session)
		{
			log.Debug("Setting up transaction");
			transactions.Add(session.Transaction);
			if (begun)
			{
                if(isoLevel == IsolationLevel.Unspecified)
                {
                    session.BeginTransaction();    
                }
                else
                {
                    session.BeginTransaction(isoLevel);
                }
				
			}
			//TODO: Set Timeout
			//if (timeoutSet)
			//{
			//    session.Transaction.SetTimeout(timeout);
			//}
		}

		public void Begin()
		{
			if (begun)
			{
				return;
			}
			if (commitFailed)
			{
				throw new TransactionException("cannot re-start transaction after failed commit");
			}
			bool beginException = false;
			foreach (ITransaction t in transactions)
			{
				try
				{
					t.Begin();
				}
				catch (HibernateException he)
				{
					log.Warn("exception starting underlying transaction", he);
					beginException = true;
				}
			}
			if (beginException)
			{
				foreach (ITransaction t in transactions)
				{
					if (t.IsActive)
					{
						try
						{
							t.Rollback();
						}
						catch (HibernateException he)
						{
							//What do we do?
						}
					}
				}
				throw new TransactionException("Begin failed");
			}
			begun = true;
			committed = false;
			rolledBack = false;
		}

		public void Begin(IsolationLevel isolationLevel)
		{
			throw new NotImplementedException();
		}

		public void Commit()
		{
			if (!begun)
			{
				throw new TransactionException("Transaction not succesfully started");
			}
			log.Debug("Starting transaction commit");
			BeforeTransactionCompletion();
			bool commitException = false;
			HibernateException firstCommitException = null;
			foreach (ITransaction t in transactions)
			{
				try
				{
					t.Commit();
				}
				catch (HibernateException he)
				{
					log.Warn("exception commiting underlying transaction", he);
					commitException = true;
					// we're only going to rethrow the first commit exception we receive
					if (firstCommitException == null)
					{
						firstCommitException = he;
					}
				}
			}
			if (commitException)
			{
				commitFailed = true;
				//afterTransactionCompletion(Status.STATUS_UNKNOWN);
				throw new TransactionException("Commit failed", firstCommitException);
			}
			//afterTransactionCompletion(Status.STATUS_COMMITTED);
			committed = true;
		}

		public void Rollback()
		{
			if (!begun && !commitFailed)
			{
				throw new TransactionException("Transaction not successfully started");
			}
			bool rollbackException = false;
			HibernateException firstRollbackException = null;
			foreach (ITransaction t in transactions)
			{
				if (t.WasCommitted)
				{
					continue;
				}
				try
				{
					t.Rollback();
				}
				catch (HibernateException he)
				{
					log.Warn("exception rolling back underlying transaction", he);
					rollbackException = true;
					if (firstRollbackException == null)
					{
						firstRollbackException = he;
					}
				}
			}
			if (rollbackException)
			{
				//we're only going to rethrow the first rollback exception
				throw new TransactionException("Rollback failed", firstRollbackException);
			}
			rolledBack = true;
		}
		
		public void Enlist(IDbCommand command)
		{
			foreach (ITransaction t in transactions)
			{
				//TODO: Should I finish with the first exception?
				try
				{
					t.Enlist(command);
				}
				catch(Exception ex)
				{
					string ExceptionMessage = "Can't enlist a commmand succesfully";
					log.Warn(ExceptionMessage);
					throw new TransactionException(ExceptionMessage, ex);
				}
			}
		}

		public void RegisterSynchronization(ISynchronization synchronization)
		{
			throw new NotImplementedException();
		}

		public bool IsActive
		{
			get { return begun && !(rolledBack || committed || commitFailed); }
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
			Exception firstException = null;
			string ExceptionMessage = "Can't dispose properly";

			foreach (ITransaction t in transactions)
			{
				try
				{
					t.Dispose();
				}
				catch (Exception ex)
				{
					log.Warn(ExceptionMessage);
					firstException = ex;
				}
			}

			if(firstException != null)
			{
				throw new TransactionException(ExceptionMessage, firstException);
			}
		}

		#endregion

		private void BeforeTransactionCompletion()
		{
			//if (synchronizations != null)
			//{
			//    foreach (Synchronization sync in synchronizations)
			//    {
			//        try
			//        {
			//            sync.BeforeCompletion();
			//        }
			//        catch (Exception t)
			//        {
			//            log.Warn("exception calling user Synchronization", t);
			//        }
			//    }
			//}
		}

		private void AfterTransactionCompletion(int status)
		{
			//begun = false;
			//if (synchronizations != null)
			//{
			//    foreach (Synchronization sync in synchronizations)
			//    {
			//        try
			//        {
			//            sync.afterCompletion(status);
			//        }
			//        catch (Throwable t)
			//        {
			//            log.Warn("exception calling user Synchronization", t);
			//        }
			//    }
			//}
		}

		public void SetTimeout(int seconds)
		{
			//timeoutSet = true;
			//timeout = seconds;
			//foreach(Transaction t in transactions) 
			//{
			//    t.SetTimeout(timeout);
			//}
		}
	}
}