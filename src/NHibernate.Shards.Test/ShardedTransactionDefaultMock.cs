using System;
using System.Data;
using NHibernate.Transaction;

namespace NHibernate.Shards.Test
{
    public class ShardedTransactionDefaultMock: IShardedTransaction
    {
        public virtual void Dispose()
        {
            throw new NotSupportedException();
        }

        public void Begin()
        {
            Begin(IsolationLevel.Unspecified);
        }

        public virtual void Begin(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }

        public virtual void Commit()
        {
            throw new NotSupportedException();
        }

        public virtual void Rollback()
        {
            throw new NotSupportedException();
        }

        public virtual void Enlist(ISession session)
        {
            throw new NotSupportedException();
        }

        public virtual void Enlist(IDbCommand command)
        {
            throw new NotSupportedException();
        }

        public virtual void RegisterSynchronization(ISynchronization synchronization)
        {
            throw new NotSupportedException();
        }

        public virtual bool IsActive
        {
            get { throw new NotSupportedException(); }
        }

        public virtual bool WasRolledBack
        {
            get { throw new NotSupportedException(); }
        }

        public virtual bool WasCommitted
        {
            get { throw new NotSupportedException(); }
        }

        public virtual void SetupTransaction(ISession session)
        {
            throw new NotSupportedException();
        }
    }
}
