namespace NHibernate.Shards.Multi
{
	using System;
	using NHibernate.Multi;
	using NHibernate.Shards.Query;
	using NHibernate.Shards.Strategy.Exit;

	internal class ShardedQueryBatchItem<T> : AbstractShardedQueryBatchItem
	{
		private readonly ShardedQueryImpl shardedQuery;

		public ShardedQueryBatchItem(IQuery query)
			: this(EnsureShardedQuery(query))
		{}

		/// <inheritdoc />
		public ShardedQueryBatchItem(ShardedQueryImpl shardedQuery)
		{
			this.shardedQuery = shardedQuery;
		}

		public override IExitOperationFactory ExitOperationFactory
		{
			get { return this.shardedQuery; }
		}

		/// <inheritdoc />
		public override void EstablishFor(IShard shard, IQueryBatch queryBatch)
		{
			queryBatch.Add<T>(this.shardedQuery.EstablishFor(shard));
		}

		/// <inheritdoc />
		public override void EstablishFor(IShard shard, string key, IQueryBatch queryBatch)
		{
			queryBatch.Add<T>(key, this.shardedQuery.EstablishFor(shard));
		}

		private static ShardedQueryImpl EnsureShardedQuery(IQuery query)
		{
			if (query is ShardedQueryImpl shardedQuery) return shardedQuery;
			throw new ArgumentException("Cannot add unsharded query to sharded query batch", nameof(query));
		}
	}
}