using System;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Multi;
using NHibernate.Shards.Criteria;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Multi
{
	internal class ShardedCriteriaBatchItem<T> : ShardedBatchItem<T>
	{
		private readonly ShardedCriteriaImpl shardedCriteria;

		public ShardedCriteriaBatchItem(ICriteria criteria)
			: this(EnsureShardedCriteria(criteria))
		{ }

		/// <inheritdoc />
		public ShardedCriteriaBatchItem(ShardedCriteriaImpl shardedCriteria)
		{
			this.shardedCriteria = shardedCriteria;
		}

		/// <inheritdoc />
		public override void EstablishFor(IShard shard, IQueryBatch queryBatch, string key = null)
		{
			if (key != null)
			{
				queryBatch.Add<T>(key, this.shardedCriteria.EstablishFor(shard));
			}
			else
			{
				queryBatch.Add<T>(this.shardedCriteria.EstablishFor(shard));
			}
		}

		/// <inheritdoc />
		public override void ExecuteNonBatched()
		{
			ProcessFinalResults(this.shardedCriteria.List<T>());
		}

		/// <inheritdoc />
		public override async Task ExecuteNonBatchedAsync(CancellationToken cancellationToken)
		{
			ProcessFinalResults(await this.shardedCriteria.ListAsync<T>(cancellationToken).ConfigureAwait(false));
		}

		/// <inheritdoc />
		public override void ProcessResults(IShardedQueryBatchImplementor queryBatch, int queryIndex)
		{
			var exitStrategy = new ListExitStrategy<T>(this.shardedCriteria);
			var results = queryBatch.GetResults(queryIndex, exitStrategy);
			ProcessFinalResults(results);
		}

		/// <inheritdoc />
		public override async Task ProcessResultsAsync(IShardedQueryBatchImplementor queryBatch, int queryIndex, CancellationToken cancellationToken)
		{
			var exitStrategy = new ListExitStrategy<T>(this.shardedCriteria);
			var results = await queryBatch.GetResultsAsync(queryIndex, exitStrategy, cancellationToken).ConfigureAwait(false);
			ProcessFinalResults(results);
		}

		private static ShardedCriteriaImpl EnsureShardedCriteria(ICriteria criteria)
		{
			if (criteria is ShardedCriteriaImpl shardedCriteria) return shardedCriteria;
			throw new ArgumentException("Cannot add unsharded criteria to sharded query batch", nameof(criteria));
		}
	}
}