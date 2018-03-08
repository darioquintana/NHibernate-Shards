namespace NHibernate.Shards.Test.Transaction
{
	using System;
	using System.Data;
	using System.Data.Common;
	using System.Threading;
	using System.Threading.Tasks;
	using NHibernate.Transaction;

	public class ShardedTransactionDefaultMock: IShardedTransaction
	{
		public virtual void Dispose()
		{
			throw new NotSupportedException();
		}

		public Task CommitAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			throw new NotSupportedException();
		}

		public Task RollbackAsync(CancellationToken cancellationToken = new CancellationToken())
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

		public void Enlist(DbCommand command)
		{
			throw new NotImplementedException();
		}

		public virtual void Enlist(ISession session)
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
