using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Multi;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Query;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Multi
{
	public class ShardedQueryBatchImpl : IShardedQueryBatch
	{
		#region Instance fields

		private readonly IShardedSessionImplementor session;

		private readonly IDictionary<IShard, IQueryBatch> establishedQueryBatchesByShard = new Dictionary<IShard, IQueryBatch>();
		private readonly ICollection<Action<IQueryBatch>> establishActions = new List<Action<IQueryBatch>>();
		private readonly IList<ShardedBatchItem> shardedBatchItems = new List<ShardedBatchItem>();

		#endregion

		#region Constructor(s)

		/// <summary>
		/// Creates new <see cref="ShardedQueryImpl"/> instance.
		/// </summary>
		/// <param name="session">The Sharded session on which this query is to be executed.</param>
		public ShardedQueryBatchImpl(IShardedSessionImplementor session)
		{
			this.session = session;
		}

		#endregion

		#region Properties

		private IQueryBatch AnyQueryBatch
		{
			get { return EstablishFor(this.session.AnyShard); }
		}

		#endregion

		#region IQueryBatch implementation

		/// <inheritdoc />
		public bool IsExecutedOrEmpty
		{
			get { return this.AnyQueryBatch.IsExecutedOrEmpty; }
		}

		/// <inheritdoc />
		public int? Timeout
		{
			get { return this.AnyQueryBatch.Timeout; }
			set { ApplyActionToShards(b => b.Timeout = value); }
		}

		/// <inheritdoc />
		public FlushMode? FlushMode
		{
			get { return this.AnyQueryBatch.FlushMode; }
			set { ApplyActionToShards(b => b.FlushMode = value); }
		}

		/// <inheritdoc />
		public void Add(IQueryBatchItem query)
		{
			AddBatchItem(query);
		}

		/// <inheritdoc />
		public void Add(string key, IQueryBatchItem query)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));
			AddBatchItem(query, key);
		}

		/// <inheritdoc />
		public void Execute()
		{
			this.session.Execute(new ExecuteShardOperation(this));
		}

		/// <inheritdoc />
		public Task ExecuteAsync(CancellationToken cancellationToken = default)
		{
			return this.session.ExecuteAsync(new ExecuteShardOperation(this), cancellationToken);
		}

		/// <inheritdoc />
		public IList<TResult> GetResult<TResult>(int queryIndex)
		{
			var shardedBatchItem = this.shardedBatchItems[queryIndex];
			var shardOperation = new GetResultShardOperation<TResult>(this, queryIndex);
			var exitStrategy = new ListExitStrategy<TResult>(shardedBatchItem.ExitOperationFactory);
			return this.session.Execute(shardOperation, exitStrategy).ToList();
		}

		/// <inheritdoc />
		public async Task<IList<TResult>> GetResultAsync<TResult>(int queryIndex, CancellationToken cancellationToken = default)
		{
			var shardedBatchItem = this.shardedBatchItems[queryIndex];
			var shardOperation = new GetResultShardOperation<TResult>(this, queryIndex);
			var exitStrategy = new ListExitStrategy<TResult>(shardedBatchItem.ExitOperationFactory);
			var result = await this.session.ExecuteAsync(shardOperation, exitStrategy, cancellationToken);
			return result.ToList();
		}

		/// <inheritdoc />
		public IList<TResult> GetResult<TResult>(string queryKey)
		{
			return GetResult<TResult>(GetQueryIndex(queryKey));
		}

		/// <inheritdoc />
		public Task<IList<TResult>> GetResultAsync<TResult>(string queryKey, CancellationToken cancellationToken = default)
		{
			return GetResultAsync<TResult>(GetQueryIndex(queryKey), cancellationToken);
		}

		#endregion

		#region Methods

		public IQueryBatch EstablishFor(IShard shard)
		{
			if (!this.establishedQueryBatchesByShard.TryGetValue(shard, out var queryBatch))
			{
				queryBatch = shard.EstablishSession().CreateQueryBatch();

				foreach (var shardedBatchItem in this.shardedBatchItems)
				{
					shardedBatchItem.EstablishFor(shard, queryBatch);
				}

				foreach (var action in this.establishActions)
				{
					action(queryBatch);
				}

				this.establishedQueryBatchesByShard.Add(shard, queryBatch);
			}
			return queryBatch;
		}

		#endregion

		#region Private methods

		private void AddBatchItem(IQueryBatchItem query, string queryKey = null)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			var shardedQuery = query as IShardedQueryBatchItemImplementor;
			if (shardedQuery == null)
			{
				throw new ArgumentException($"An unsharded query cannot be added to a sharded query batch.", nameof(query));
			}

			this.shardedBatchItems.Add(new ShardedBatchItem(queryKey, shardedQuery));

			foreach (var pair in this.establishedQueryBatchesByShard)
			{
				shardedQuery.EstablishFor(pair.Key, pair.Value);
			}
		}

		private void ApplyActionToShards(Action<IQueryBatch> action)
		{
			this.establishActions.Add(action);
			foreach (var queryBatch in this.establishedQueryBatchesByShard.Values)
			{
				action(queryBatch);
			}
		}

		private int GetQueryIndex(string queryKey)
		{
			for (var i = 0; i < this.shardedBatchItems.Count; i++)
			{
				if (this.shardedBatchItems[i].Key == queryKey) return i;
			}
			throw new KeyNotFoundException($"No query found with key '{queryKey}'.");
		}

		#endregion

		#region Inner classes

		private struct ShardedBatchItem
		{
			public readonly string Key;
			public readonly IShardedQueryBatchItemImplementor ShardedQuery;

			public ShardedBatchItem(string key, IShardedQueryBatchItemImplementor shardedQuery)
			{
				this.Key = key;
				this.ShardedQuery = shardedQuery;
			}

			public IExitOperationFactory ExitOperationFactory
			{
				get {  return this.ShardedQuery.ExitOperationFactory; }
			}

			public void EstablishFor(IShard shard, IQueryBatch queryBatch)
			{
				if (this.Key == null)
				{
					this.ShardedQuery.EstablishFor(shard, queryBatch);
				}
				else
				{
					this.ShardedQuery.EstablishFor(shard, this.Key, queryBatch);
				}
			}
		}

		private class ExecuteShardOperation : IShardOperation, IAsyncShardOperation
		{
			private readonly IShardedQueryBatch shardedQueryBatch;

			public ExecuteShardOperation(IShardedQueryBatch shardedQueryBatch)
			{
				this.shardedQueryBatch = shardedQueryBatch;
			}

			public System.Action Prepare(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var queryBatch = this.shardedQueryBatch.EstablishFor(shard);
				return queryBatch.Execute;
			}

			public Func<CancellationToken, Task> PrepareAsync(IShard shard)
			{
				var queryBatch = this.shardedQueryBatch.EstablishFor(shard);
				return queryBatch.ExecuteAsync;
			}

			public string OperationName
			{
				get { return "Execute()"; }
			}
		}

		private class GetResultShardOperation<T> : IShardOperation<IEnumerable<T>>, IAsyncShardOperation<IEnumerable<T>>
		{
			private readonly IShardedQueryBatch shardedQueryBatch;
			private readonly int resultIndex;

			public GetResultShardOperation(IShardedQueryBatch shardedQueryBatch, int resultIndex)
			{
				this.shardedQueryBatch = shardedQueryBatch;
				this.resultIndex = resultIndex;
			}

			public Func<IEnumerable<T>> Prepare(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var multiQuery = this.shardedQueryBatch.EstablishFor(shard);
				return () => multiQuery.GetResult<T>(this.resultIndex);
			}

			public Func<CancellationToken, Task<IEnumerable<T>>> PrepareAsync(IShard shard)
			{
				var multiQuery = this.shardedQueryBatch.EstablishFor(shard);
				return async ct => await multiQuery.GetResultAsync<T>(this.resultIndex, ct);
			}

			public string OperationName
			{
				get { return $"GetResult({this.resultIndex})"; }
			}
		}

		#endregion
	}
}
