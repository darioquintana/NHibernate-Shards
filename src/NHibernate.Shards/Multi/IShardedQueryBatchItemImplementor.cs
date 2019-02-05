using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Multi;

namespace NHibernate.Shards.Multi
{
	public interface IShardedQueryBatchItemImplementor
	{
		void EstablishFor(IShard shard, IQueryBatch queryBatch, string key = null);
		void ProcessResults(IShardedQueryBatchImplementor queryBatch, int queryIndex);
		Task ProcessResultsAsync(IShardedQueryBatchImplementor queryBatch, int queryIndex, CancellationToken cancellationToken);
	}

	public interface IShardedQueryBatchItemImplementor<T> : IShardedQueryBatchItemImplementor
	{
		IList<T> GetResults();
	}
}