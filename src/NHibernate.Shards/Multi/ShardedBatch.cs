using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Multi;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Query;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Multi
{
	public class ShardedBatch : IShardedQueryBatch, IShardedQueryBatchImplementor
	{
		#region Instance fields

		private readonly IShardedSessionImplementor session;
		private readonly IDictionary<IShard, IQueryBatch> establishedQueryBatchesByShard = new Dictionary<IShard, IQueryBatch>();
		private readonly ICollection<Action<IQueryBatch>> establishActions = new List<Action<IQueryBatch>>();
		private readonly IList<Entry> entries = new List<Entry>();
		private bool executed;

		#endregion

		#region Constructor(s)

		/// <summary>
		/// Creates new <see cref="ShardedQueryImpl"/> instance.
		/// </summary>
		/// <param name="session">The Sharded session on which this query is to be executed.</param>
		public ShardedBatch(IShardedSessionImplementor session)
		{
			this.session = session;
		}

		#endregion

		#region Properties

		public int Count
		{
			get { return this.entries.Count; }
		}

		private IQueryBatch AnyQueryBatch
		{
			get { return EstablishFor(this.session.AnyShard); }
		}

		#endregion

		#region IQueryBatch implementation

		/// <inheritdoc />
		public bool IsExecutedOrEmpty
		{
			get { return this.entries.Count == 0 || this.executed; }
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
			this.executed = true;
			this.session.Execute(new ExecuteShardOperation(this));

			for (var queryIndex = 0; queryIndex < this.entries.Count; queryIndex++)
			{
				var entry = this.entries[queryIndex];
				entry.ShardedBatchItem.ProcessResults(this, queryIndex);
			}
		}

		/// <inheritdoc />
		public async Task ExecuteAsync(CancellationToken cancellationToken = default)
		{
			this.executed = true;
			await this.session.ExecuteAsync(new ExecuteShardOperation(this), cancellationToken).ConfigureAwait(false);

			for (var queryIndex = 0; queryIndex < this.entries.Count; queryIndex++)
			{
				var entry = this.entries[queryIndex];
				await entry.ShardedBatchItem.ProcessResultsAsync(this, queryIndex, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public IList<TResult> GetResult<TResult>(int queryIndex)
		{
			var entry = this.entries[queryIndex];
			if (!(entry.ShardedBatchItem is IShardedQueryBatchItemImplementor<TResult> shardedBatchItem))
			{
				throw new ArgumentException("Invalid query result type.");
			}

			if (!this.executed)
			{
				Execute();
			}
			return shardedBatchItem.GetResults();
		}

		/// <inheritdoc />
		public async Task<IList<TResult>> GetResultAsync<TResult>(int queryIndex, CancellationToken cancellationToken = default)
		{
			var entry = this.entries[queryIndex];
			if (!(entry.ShardedBatchItem is IShardedQueryBatchItemImplementor<TResult> shardedBatchItem))
			{
				throw new ArgumentException("Invalid query result type.");
			}

			if (!this.executed)
			{
				await ExecuteAsync(cancellationToken).ConfigureAwait(false);
			}
			return shardedBatchItem.GetResults();
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

		#region IShardedQueryBatchImplementor implementation

		/// <inheritdoc />
		public IList<TResult> GetResults<TResult>(int queryIndex, IListExitStrategy<TResult> exitStrategy)
		{
			var operation = new GetResultShardOperation<TResult>(this, queryIndex);
			var results = this.session.Execute(operation, exitStrategy);
			return results as IList<TResult> ?? new List<TResult>(results);
		}

		/// <inheritdoc />
		public async Task<IList<TResult>> GetResultsAsync<TResult>(int queryIndex, IListExitStrategy<TResult> exitStrategy, CancellationToken cancellationToken = default)
		{
			var operation = new GetResultShardOperation<TResult>(this, queryIndex);
			var results = await this.session.ExecuteAsync(operation, exitStrategy, cancellationToken).ConfigureAwait(false);
			return results as IList<TResult> ?? new List<TResult>(results);
		}

		#endregion

		#region Methods

		private IQueryBatch EstablishFor(IShard shard)
		{
			if (!this.establishedQueryBatchesByShard.TryGetValue(shard, out var queryBatch))
			{
				queryBatch = shard.EstablishSession().CreateQueryBatch();

				foreach (var entry in this.entries)
				{
					entry.ShardedBatchItem.EstablishFor(shard, queryBatch, entry.Key);
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

			this.entries.Add(new Entry(queryKey, shardedQuery));

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
			for (var i = 0; i < this.entries.Count; i++)
			{
				if (this.entries[i].Key == queryKey) return i;
			}
			throw new KeyNotFoundException($"No query found with key '{queryKey}'.");
		}

		#endregion

		#region Inner classes

		private struct Entry
		{
			public readonly string Key;
			public readonly IShardedQueryBatchItemImplementor ShardedBatchItem;

			public Entry(string key, IShardedQueryBatchItemImplementor shardedBatchItem)
			{
				this.Key = key;
				this.ShardedBatchItem = shardedBatchItem;
			}
		}

		private class ExecuteShardOperation : IShardOperation, IAsyncShardOperation
		{
			private readonly ShardedBatch shardedQueryBatch;

			public ExecuteShardOperation(ShardedBatch shardedQueryBatch)
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
			private readonly ShardedBatch shardedQueryBatch;
			private readonly int queryIndex;

			public GetResultShardOperation(ShardedBatch shardedQueryBatch, int queryIndex)
			{
				this.shardedQueryBatch = shardedQueryBatch;
				this.queryIndex = queryIndex;
			}

			public Func<IEnumerable<T>> Prepare(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var multiQuery = this.shardedQueryBatch.EstablishFor(shard);
				return () => multiQuery.GetResult<T>(this.queryIndex);
			}

			public Func<CancellationToken, Task<IEnumerable<T>>> PrepareAsync(IShard shard)
			{
				var multiQuery = this.shardedQueryBatch.EstablishFor(shard);
				return async ct => await multiQuery.GetResultAsync<T>(this.queryIndex, ct).ConfigureAwait(false);
			}

			public string OperationName
			{
				get { return $"GetResult({this.queryIndex})"; }
			}
		}

		#endregion
	}
}
