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

		protected override IListExitStrategy<T> BuildListExitStrategy()
		{
			return new ListExitStrategy<T>(this.shardedCriteria);
		}

		private static ShardedCriteriaImpl EnsureShardedCriteria(ICriteria criteria)
		{
			if (criteria is ShardedCriteriaImpl shardedCriteria) return shardedCriteria;
			throw new ArgumentException("Cannot add unsharded criteria to sharded query batch", nameof(criteria));
		}
	}
}