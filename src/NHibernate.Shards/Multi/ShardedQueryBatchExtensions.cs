using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Linq;
using NHibernate.Shards.Linq;
using NHibernate.Shards.Multi;
using NHibernate.Shards.Query;
using Remotion.Linq.Parsing.ExpressionVisitors;

namespace NHibernate.Multi
{
	public static class ShardedQueryBatchExtensions
	{
		#region Extension methods - HQL batching

		/// <summary>
		/// Adds a query to the sharded batch.
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
		/// Adds a query to the sharded batch.
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
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureEnumerable{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureEnumerable<TResult> AddAsFuture<TResult>(this IShardedQueryBatch shardedBatch, IQuery query)
		{
			return AddAsFuture(shardedBatch, new ShardedQueryBatchItem<TResult>(query));
		}

		/// <summary>
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureValue{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureValue<TResult> AddAsFutureValue<TResult>(this IShardedQueryBatch shardedBatch, IQuery query)
		{
			return AddAsFutureValue(shardedBatch, new ShardedQueryBatchItem<TResult>(query));
		}

		#endregion

		#region Extension methods - Linq batching

		/// <summary>
		/// Adds a query to the sharded batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <param name="afterLoad">Callback to execute when query is loaded. Loaded results are provided as action parameter.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TResult>(this IShardedQueryBatch shardedBatch, IQueryable<TResult> query, Action<IList<TResult>> afterLoad = null)
		{
			shardedBatch.Add(new ShardedQueryBatchItem<TResult>(ToShardedQuery(query)) { AfterLoadCallback = afterLoad });
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the sharded batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="key">A key for retrieval of the query result.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TResult>(this IShardedQueryBatch shardedBatch, string key, IQueryable<TResult> query)
		{
			shardedBatch.Add(key, new ShardedQueryBatchItem<TResult>(ToShardedQuery(query)));
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the sharded batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <param name="selector">An aggregation function to apply to <paramref name="query"/>.</param>
		/// <param name="afterLoad">Callback to execute when query is loaded. Loaded results are provided as action parameter.</param>
		/// <typeparam name="TSource">The type of the query elements before aggregation.</typeparam>
		/// <typeparam name="TResult">The type resulting of the query result aggregation.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TSource, TResult>(this IShardedQueryBatch shardedBatch, 
			IQueryable<TSource> query, Expression<Func<IQueryable<TSource>, TResult>> selector, Action<TResult> afterLoad = null)
		{
			var batchItem = new ShardedQueryBatchItem<TResult>(ToShardedQuery(query, selector));
			if (afterLoad != null)
			{
				batchItem.AfterLoadCallback += list => afterLoad(list[0]);
			}
			shardedBatch.Add(batchItem);
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the sharded batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="key">A key for retrieval of the query result.</param>
		/// <param name="query">The query.</param>
		/// <param name="selector">An aggregation function to apply to <paramref name="query"/>.</param>
		/// <typeparam name="TSource">The type of the query elements before aggregation.</typeparam>
		/// <typeparam name="TResult">The type resulting of the query result aggregation.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TSource, TResult>(this IShardedQueryBatch shardedBatch, string key, 
			IQueryable<TSource> query, Expression<Func<IQueryable<TSource>, TResult>> selector)
		{
			shardedBatch.Add(new ShardedQueryBatchItem<TResult>(ToShardedQuery(query, selector)));
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureEnumerable{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureEnumerable<TResult> AddAsFuture<TResult>(this IShardedQueryBatch shardedBatch, IQueryable<TResult> query)
		{
			return AddAsFuture(shardedBatch, new ShardedQueryBatchItem<TResult>(ToShardedQuery(query)));
		}

		/// <summary>
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureValue{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureValue<TResult> AddAsFutureValue<TResult>(this IShardedQueryBatch shardedBatch, IQueryable<TResult> query)
		{
			return AddAsFutureValue(shardedBatch, new ShardedQueryBatchItem<TResult>(ToShardedQuery(query)));
		}

		/// <summary>
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureValue{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <param name="selector">An aggregation function to apply to <paramref name="query"/>.</param>
		/// <typeparam name="TSource">The type of the query elements before aggregation.</typeparam>
		/// <typeparam name="TResult">The type resulting of the query result aggregation.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureValue<TResult> AddAsFutureValue<TSource, TResult>(this IShardedQueryBatch shardedBatch, 
			IQueryable<TSource> query, Expression<Func<IQueryable<TSource>, TResult>> selector)
		{
			return AddAsFutureValue(shardedBatch, new ShardedQueryBatchItem<TResult>(ToShardedQuery(query, selector)));
		}

		private static ShardedQueryImpl ToShardedQuery<TResult>(IQueryable<TResult> query)
		{
			switch (query)
			{
				case null:
					throw new ArgumentNullException(nameof(query));
				case NhQueryable<TResult> nhQueryable when nhQueryable.Provider is ShardedQueryProvider shardedProvider:
					return (ShardedQueryImpl)shardedProvider.GetPreparedQuery(query.Expression, out _);
				default:
					throw new ArgumentException("Cannot add unsharded Linq query to sharded query batch", nameof(query));
			}
		}

		private static ShardedQueryImpl ToShardedQuery<TSource, TResult>(IQueryable<TSource> query, Expression<Func<IQueryable<TSource>, TResult>> selector)
		{
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			switch (query)
			{
				case null:
					throw new ArgumentNullException(nameof(query));
				case NhQueryable<TSource> nhQueryable when nhQueryable.Provider is ShardedQueryProvider shardedProvider:
				{
					var expression = ReplacingExpressionVisitor.Replace(selector.Parameters.Single(), query.Expression, selector.Body);
					return (ShardedQueryImpl)shardedProvider.GetPreparedQuery(expression, out _);
				}
				default:
					throw new ArgumentException("Cannot add unsharded linq query to sharded query batch", nameof(query));
			}
		}

		#endregion

		#region Extension methods - Criteria batching

		/// <summary>
		/// Adds a query to the sharded batch.
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
		/// Adds a query to the sharded batch.
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
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureEnumerable{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureEnumerable<TResult> AddAsFuture<TResult>(this IShardedQueryBatch shardedBatch, ICriteria query)
		{
			return AddAsFuture(shardedBatch, new ShardedCriteriaBatchItem<TResult>(query));
		}

		/// <summary>
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureValue{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureValue<TResult> AddAsFutureValue<TResult>(this IShardedQueryBatch shardedBatch, ICriteria query)
		{
			return AddAsFutureValue(shardedBatch, new ShardedCriteriaBatchItem<TResult>(query));
		}

		#endregion

		#region Extension methods - Queryover batching

		/// <summary>
		/// Adds a query to the sharded batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <param name="afterLoad">Callback to execute when query is loaded. Loaded results are provided as action parameter.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TResult>(this IShardedQueryBatch shardedBatch, IQueryOver query, Action<IList<TResult>> afterLoad = null)
		{
			shardedBatch.Add(new ShardedQueryOverBatchItem<TResult>(query) { AfterLoadCallback = afterLoad });
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the sharded batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="key">A key for retrieval of the query result.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TResult>(this IShardedQueryBatch shardedBatch, string key, IQueryOver query)
		{
			shardedBatch.Add(key, new ShardedQueryOverBatchItem<TResult>(query));
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the sharded batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <param name="afterLoad">Callback to execute when query is loaded. Loaded results are provided as action parameter.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TResult>(this IShardedQueryBatch shardedBatch, IQueryOver<TResult> query, Action<IList<TResult>> afterLoad = null)
		{
			shardedBatch.Add(new ShardedQueryOverBatchItem<TResult>(query) { AfterLoadCallback = afterLoad });
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the sharded batch.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="key">A key for retrieval of the query result.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if the batch has already been executed.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> is <see langword="null"/>.</exception>
		/// <returns>The batch instance for method chain.</returns>
		public static IShardedQueryBatch Add<TResult>(this IShardedQueryBatch shardedBatch, string key, IQueryOver<TResult> query)
		{
			shardedBatch.Add(key, new ShardedQueryOverBatchItem<TResult>(query));
			return shardedBatch;
		}

		/// <summary>
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureEnumerable{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureEnumerable<TResult> AddAsFuture<TResult>(this IShardedQueryBatch shardedBatch, IQueryOver query)
		{
			return AddAsFuture(shardedBatch, new ShardedQueryOverBatchItem<TResult>(query));
		}

		/// <summary>
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureEnumerable{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureEnumerable<TResult> AddAsFuture<TResult>(this IShardedQueryBatch shardedBatch, IQueryOver<TResult> query)
		{
			return AddAsFuture(shardedBatch, new ShardedQueryOverBatchItem<TResult>(query));
		}

		/// <summary>
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureValue{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureValue<TResult> AddAsFutureValue<TResult>(this IShardedQueryBatch shardedBatch, IQueryOver query)
		{
			return AddAsFutureValue(shardedBatch, new ShardedQueryOverBatchItem<TResult>(query));
		}

		/// <summary>
		/// Adds a query to the sharded batch, returning it as an <see cref="IFutureValue{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="query">The query.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureValue<TResult> AddAsFutureValue<TResult>(this IShardedQueryBatch shardedBatch, IQueryOver<TResult> query)
		{
			return AddAsFutureValue(shardedBatch, new ShardedQueryOverBatchItem<TResult>(query));
		}

		#endregion

		#region Extension methods - Batch item futures

		/// <summary>
		/// Adds an item to the sharded batch, returning it as an <see cref="IFutureEnumerable{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="shardedBatchItem">The batch item.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureEnumerable<TResult> AddAsFuture<TResult>(this IShardedQueryBatch shardedBatch, IQueryBatchItem<TResult> shardedBatchItem)
		{
			shardedBatch.Add(shardedBatchItem);
			return new FutureResult<TResult>(shardedBatch, shardedBatch.Count);
		}

		/// <summary>
		/// Adds an item to the sharded batch, returning it as an <see cref="IFutureValue{T}"/>.
		/// </summary>
		/// <param name="shardedBatch">The batch.</param>
		/// <param name="shardBatchItem">The batch item.</param>
		/// <typeparam name="TResult">The type of the query result elements.</typeparam>
		/// <returns>A future query which execution will be handled by the batch.</returns>
		public static IFutureValue<TResult> AddAsFutureValue<TResult>(this IShardedQueryBatch shardedBatch, IQueryBatchItem<TResult> shardBatchItem)
		{
			shardedBatch.Add(shardBatchItem);
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
