using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Shards.Multi;

namespace NHibernate.Multi
{
	public static class ShardedQueryBatchExtensions
	{
		#region Extension methods - HQL batching

		/// <summary>
		/// Adds a query to the batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <param name="afterLoad">Callback to execute when query is loaded. Loaded results are provided as action parameter.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TResult>(this IShardedQueryBatch shardedBatch, IQuery query, Action<IList<TResult>> afterLoad = null)
		{
			shardedBatch.Add(new ShardedQueryBatchItem<TResult>(query) { AfterLoadCallback = afterLoad });
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="key">A key for retrieval of the query result.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TResult>(this IShardedQueryBatch shardedBatch, string key, IQuery query)
		{
			shardedBatch.Add(key, new ShardedQueryBatchItem<TResult>(query));
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the batch, returning it as an <see cref="IFutureEnumerable{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureEnumerable<TResult> AddAsFuture<TResult>(this IShardedQueryBatch shardedBatch, IQuery query)
		{
			shardedBatch.Add(new ShardedQueryBatchItem<TResult>(query));
			return new FutureResult<TResult>(shardedBatch, shardedBatch.Count);
		}

		/// <summary>
		/// Adds a query to the batch, returning it as an <see cref="IFutureValue{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureValue<TResult> AddAsFutureValue<TResult>(this IShardedQueryBatch shardedBatch, IQuery query)
		{
			shardedBatch.Add(new ShardedQueryBatchItem<TResult>(query));
			return new FutureResult<TResult>(shardedBatch, shardedBatch.Count);
		}

		#endregion

		#region Extension methods - Criteria batching

		/// <summary>
		/// Adds a query to the batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <param name="afterLoad">Callback to execute when query is loaded. Loaded results are provided as action parameter.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TResult>(this IShardedQueryBatch shardedBatch, ICriteria query, Action<IList<TResult>> afterLoad = null)
		{
			shardedBatch.Add(new ShardedCriteriaBatchItem<TResult>(query) { AfterLoadCallback = afterLoad });
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="key">A key for retrieval of the query result.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TResult>(this IShardedQueryBatch shardedBatch, string key, ICriteria query)
		{
			shardedBatch.Add(key, new ShardedCriteriaBatchItem<TResult>(query));
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the batch, returning it as an <see cref="IFutureEnumerable{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureEnumerable<TResult> AddAsFuture<TResult>(this IShardedQueryBatch shardedBatch, ICriteria query)
		{
			shardedBatch.Add(new ShardedCriteriaBatchItem<TResult>(query));
			return new FutureResult<TResult>(shardedBatch, shardedBatch.Count);
		}

		/// <summary>
		/// Adds a query to the batch, returning it as an <see cref="IFutureValue{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureValue<TResult> AddAsFutureValue<TResult>(this IShardedQueryBatch shardedBatch, ICriteria query)
		{
			shardedBatch.Add(new ShardedCriteriaBatchItem<TResult>(query));
			return new FutureResult<TResult>(shardedBatch, shardedBatch.Count);
		}

		#endregion

		#region Inner types

		private class FutureResult<T> : IFutureEnumerable<T>, IFutureValue<T>
		{
			private readonly IQueryBatch queryBatch;
			private readonly int queryIndex;

			public FutureResult(IQueryBatch queryBatch, int queryIndex)
			{
				this.queryBatch = queryBatch;
				this.queryIndex = queryIndex;
			}

			#region IFutureEnumerable<T> implementation

			/// <inheritdoc />
			public IEnumerable<T> GetEnumerable()
			{
				return this.queryBatch.GetResult<T>(this.queryIndex);
			}

			/// <inheritdoc />
			public Task<IEnumerable<T>> GetEnumerableAsync(CancellationToken cancellationToken = new CancellationToken())
			{
				return this.queryBatch.GetResultAsync<T>(this.queryIndex, cancellationToken)
					.ContinueWith(t => (IEnumerable<T>)t.Result,
						TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
			}

			public IEnumerator<T> GetEnumerator()
			{
				return GetEnumerable().GetEnumerator();
			}

			/// <inheritdoc />
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			#endregion

			#region IFutureValue<T> implementation

			/// <inheritdoc />
			public T Value
			{
				get
				{
					var list = this.queryBatch.GetResult<T>(this.queryIndex);
					return list.Count > 0 ? list[0] : default;
				}
			}

			/// <inheritdoc />
			public Task<T> GetValueAsync(CancellationToken cancellationToken = default)
			{
				return this.queryBatch.GetResultAsync<T>(this.queryIndex, cancellationToken)
					.ContinueWith(t => t.Result.Count > 0 ? t.Result[0] : default, 
						TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
			}

			#endregion
		}

		#endregion
	}
}
