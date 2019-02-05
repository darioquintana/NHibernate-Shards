using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Linq;
using NHibernate.Shards.Linq;
using NHibernate.Shards.Query;
using Remotion.Linq.Parsing.ExpressionVisitors;

namespace NHibernate.Shards.Multi
{
	internal class ShardedLinqBatchItem<TResult> : ShardedQueryBatchItem<TResult>
	{
		public ShardedLinqBatchItem(IQueryable<TResult> query)
			: base(ToShardedQuery(query))
		{}

		private static ShardedQueryImpl ToShardedQuery(IQueryable<TResult> query)
		{
			switch (query)
			{
				case null:
					throw new ArgumentNullException(nameof(query));
				case NhQueryable<TResult> nhQueryable when nhQueryable.Provider is ShardedQueryProvider shardedProvider:
					return (ShardedQueryImpl)shardedProvider.GetPreparedQuery(query.Expression, out _);
				default:
					throw new ArgumentException("Cannot add unsharded linq query to sharded query batch", nameof(query));
			}
		}
	}

//	internal class ShardedLinqBatchItem<TSource, TResult> : ShardedQueryBatchItem<TSource, TResult>
//	{
//		public ShardedLinqBatchItem(IQueryable<TSource> query, Expression<Func<IQueryable<TSource>, TResult>> selector)
//			: base(ToShardedQuery(query, selector))
//		{}

//		/// <inheritdoc />
//		protected  override IList<TResult> TransformResults(IList<TSource> results)
//		{
//			var postExecuteTransformer = nhLinqExpression.ExpressionToHqlTranslationResults.PostExecuteTransformer;
//			if (postExecuteTransformer == null)
//			{
//				return typeof(TResult) == typeof(TSource)
//					? (IList<TResult>) results
//					: new List<TResult>(results.Cast<TResult>());
//			}

//			var transformResult = (TResult)postExecuteTransformer.DynamicInvoke(results.AsQueryable());
//			return new[] { transformResult };
//		}

//		private static ShardedQueryImpl ToShardedQuery(IQueryable<TSource> query, Expression<Func<IQueryable<TSource>, TResult>> selector)
//		{
//			if (selector == null) throw new ArgumentNullException(nameof(selector));

//			switch (query)
//			{
//				case null:
//					throw new ArgumentNullException(nameof(query));
//				case NhQueryable<TSource> nhQueryable when nhQueryable.Provider is ShardedQueryProvider shardedProvider:
//				{
//					var expression = ReplacingExpressionVisitor.Replace(selector.Parameters.Single(), query.Expression, selector.Body);
//					return (ShardedQueryImpl)shardedProvider.GetPreparedQuery(expression, out _);
//				}
//				default:
//					throw new ArgumentException("Cannot add unsharded linq query to sharded query batch", nameof(query));
//			}
//		}
//	}
}