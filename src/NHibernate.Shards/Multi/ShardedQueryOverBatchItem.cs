using System;
using NHibernate.Shards.Criteria;

namespace NHibernate.Shards.Multi
{
	internal class ShardedQueryOverBatchItem<TResult> : ShardedCriteriaBatchItem<TResult>
	{
		public ShardedQueryOverBatchItem(IQueryOver queryOver)
			: base(ToShardedCriteria(queryOver))
		{ }

		private static ShardedCriteriaImpl ToShardedCriteria(IQueryOver queryOver)
		{
			if (queryOver is IShardedQueryOverImplementor shardedQueryOver) return (ShardedCriteriaImpl)shardedQueryOver.ShardedRootCriteria;
			throw new ArgumentException("Cannot add unsharded QueryOver to sharded query batch", nameof(queryOver));
		}
	}
}