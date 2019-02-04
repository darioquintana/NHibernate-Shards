using System.Collections.Generic;
using NHibernate.Multi;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Multi
{
	public interface IShardedQueryBatchItemImplementor : IQueryBatchItem
	{
		bool HasResults { get; }
		IExitOperationFactory ExitOperationFactory { get; }
		void EstablishFor(IShard shard, IQueryBatch queryBatch);
		void EstablishFor(IShard shard, string key, IQueryBatch queryBatch);
	}

	public interface IShardedQueryBatchItemImplementor<T> : IShardedQueryBatchItemImplementor, IQueryBatchItem<T>
	{
		void ProcessResults(IList<T> results);
	}
}