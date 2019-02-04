using System;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Multi;
using NHibernate.Shards.Criteria;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Multi
{
	internal class ShardedCriteriaBatchItem<T> : AbstractShardedQueryBatchItem<T>
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

		public override IExitOperationFactory ExitOperationFactory
		{
			get { return this.shardedCriteria; }
		}

		/// <inheritdoc />
		public override void EstablishFor(IShard shard, IQueryBatch queryBatch)
		{
			queryBatch.Add<T>(this.shardedCriteria.EstablishFor(shard));
		}

		/// <inheritdoc />
		public override void EstablishFor(IShard shard, string key, IQueryBatch queryBatch)
		{
			queryBatch.Add<T>(key, this.shardedCriteria.EstablishFor(shard));
		}

		/// <inheritdoc />
		public override void ExecuteNonBatched()
		{
			ProcessResults(this.shardedCriteria.List<T>());
		}

		/// <inheritdoc />
		public override async Task ExecuteNonBatchedAsync(CancellationToken cancellationToken)
		{
			ProcessResults(await this.shardedCriteria.ListAsync<T>(cancellationToken).ConfigureAwait(false));
		}

		private static ShardedCriteriaImpl EnsureShardedCriteria(ICriteria criteria)
		{
			if (criteria is ShardedCriteriaImpl shardedCriteria) return shardedCriteria;
			throw new ArgumentException("Cannot add unsharded criteria to sharded query batch", nameof(criteria));
		}
	}
}