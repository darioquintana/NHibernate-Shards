using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Multi;
using NHibernate.Shards.Query;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Multi
{
	internal abstract class ShardedQueryBatchItem<TSource, TResult> : ShardedBatchItem<TSource, TResult>
	{
		private readonly ShardedQueryImpl shardedQuery;

		/// <inheritdoc />
		protected ShardedQueryBatchItem(ShardedQueryImpl shardedQuery)
		{
			this.shardedQuery = shardedQuery;
		}

		/// <inheritdoc />
		public override void EstablishFor(IShard shard, IQueryBatch queryBatch, string key = null)
		{
			if (key != null)
			{
				queryBatch.Add<TSource>(key, this.shardedQuery.EstablishFor(shard));
			}
			else
			{
				queryBatch.Add<TSource>(this.shardedQuery.EstablishFor(shard));
			}
		}

		/// <inheritdoc />
		public override void ExecuteNonBatched()
		{
			ProcessFinalResults(this.shardedQuery.List<TSource>());
		}

		/// <inheritdoc />
		public override async Task ExecuteNonBatchedAsync(CancellationToken cancellationToken)
		{
			ProcessFinalResults(await this.shardedQuery.ListAsync<TSource>(cancellationToken).ConfigureAwait(false));
		}

		/// <inheritdoc />
		protected override IListExitStrategy<TSource> BuildListExitStrategy()
		{
			return new ListExitStrategy<TSource>(this.shardedQuery);
		}

		protected static ShardedQueryImpl EnsureShardedQuery(IQuery query)
		{
			if (query is ShardedQueryImpl shardedQuery) return shardedQuery;
			throw new ArgumentException("Cannot add unsharded query to sharded query batch", nameof(query));
		}
	}

	internal class ShardedQueryBatchItem<TResult> : ShardedQueryBatchItem<TResult, TResult>
	{
		/// <inheritdoc />
		public ShardedQueryBatchItem(IQuery query) 
			: this(EnsureShardedQuery(query))
		{ }

		/// <inheritdoc />
		public ShardedQueryBatchItem(ShardedQueryImpl shardedQuery) 
			: base(shardedQuery)
		{ }

		/// <inheritdoc />
		protected override IList<TResult> TransformResults(IList<TResult> results)
		{
			return results;
		}
	}

}
