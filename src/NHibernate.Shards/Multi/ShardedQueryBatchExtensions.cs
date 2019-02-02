using NHibernate.Shards.Multi;

namespace NHibernate.Multi
{
	public static class ShardedQueryBatchExtensions
	{
		public static IShardedQueryBatch Add<T>(this IShardedQueryBatch shardedQueryBatch, IQuery query)
		{
			shardedQueryBatch.Add(new ShardedQueryBatchItem<T>(query));
			return shardedQueryBatch;
		}

		public static IShardedQueryBatch Add<T>(this IShardedQueryBatch shardedQueryBatch, string key, IQuery query)
		{
			shardedQueryBatch.Add(key, new ShardedQueryBatchItem<T>(query));
			return shardedQueryBatch;
		}
	}
}
