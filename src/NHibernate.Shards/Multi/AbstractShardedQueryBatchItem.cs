namespace NHibernate.Shards.Multi
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using System.Threading.Tasks;
	using NHibernate.Engine;
	using NHibernate.Multi;
	using NHibernate.Shards.Strategy.Exit;
	using NHibernate.SqlCommand;

	internal abstract class AbstractShardedQueryBatchItem : IShardedQueryBatchItemImplementor
	{
		#region IShardedQueryBatchItem implementation

		/// <inheritdoc />
		public abstract IExitOperationFactory ExitOperationFactory { get; }

		/// <inheritdoc />
		public abstract void EstablishFor(IShard shard, IQueryBatch queryBatch);

		/// <inheritdoc />
		public abstract void EstablishFor(IShard shard, string key, IQueryBatch queryBatch);

		#endregion

		#region IQueryBatchItem implementation

		/// <inheritdoc />
		Task<int> IQueryBatchItem.ProcessResultsSetAsync(DbDataReader reader, CancellationToken cancellationToken)
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		Task IQueryBatchItem.ExecuteNonBatchedAsync(CancellationToken cancellationToken)
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		void IQueryBatchItem.Init(ISessionImplementor session)
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		IEnumerable<string> IQueryBatchItem.GetQuerySpaces()
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		IEnumerable<ISqlCommand> IQueryBatchItem.GetCommands()
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		int IQueryBatchItem.ProcessResultsSet(DbDataReader reader)
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		void IQueryBatchItem.ProcessResults()
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		void IQueryBatchItem.ExecuteNonBatched()
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		IEnumerable<ICachingInformation> IQueryBatchItem.CachingInformation
		{
			get { throw NotSupportedException(); }
		}

		private Exception NotSupportedException([CallerMemberName]string operationName = null)
		{
			throw new NotSupportedException($"{nameof(IQueryBatchItem)}.{operationName} is not supported on sharded query batches");
		}

		#endregion
	}
}