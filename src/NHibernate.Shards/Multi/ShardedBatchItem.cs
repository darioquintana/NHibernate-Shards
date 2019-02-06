using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Engine;
using NHibernate.Multi;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.SqlCommand;

namespace NHibernate.Shards.Multi
{
	internal abstract class ShardedBatchItem<TSource, TResult> : IQueryBatchItem<TResult>, IShardedQueryBatchItemImplementor<TResult>
	{
		#region Instance fields

		private IList<TResult> finalResults;

		#endregion

		#region IQueryBatchItem implementation

		/// <inheritdoc />
		IEnumerable<ICachingInformation> IQueryBatchItem.CachingInformation
		{
			get { throw NotSupportedException(); }
		}


		/// <inheritdoc />
		void IQueryBatchItem.Init(ISessionImplementor session)
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		void IQueryBatchItem.ProcessResults()
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		int IQueryBatchItem.ProcessResultsSet(DbDataReader reader)
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		Task<int> IQueryBatchItem.ProcessResultsSetAsync(DbDataReader reader, CancellationToken cancellationToken)
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		IEnumerable<string> IQueryBatchItem.GetQuerySpaces()
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		IEnumerable<ISqlCommand> IQueryBatchItem.GetCommands()
		{
			throw NotSupportedException();
		}

		private static Exception NotSupportedException([CallerMemberName] string memberName = null)
		{
			return new NotSupportedException($"The {nameof(IQueryBatch)}.{memberName} operation is not supported in sharded query batch items.");
		}

		#endregion

		#region IQueryBatchItem<T> implementation

		public IList<TResult> GetResults()
		{
			return this.finalResults;
		}

		/// <inheritdoc />
		public Action<IList<TResult>> AfterLoadCallback { get; set; }

		#endregion

		#region IShardedQueryBatchItemImplementor implementation

		/// <inheritdoc />
		public abstract void EstablishFor(IShard shard, IQueryBatch queryBatch, string key = null);

		/// <inheritdoc />
		public abstract void ExecuteNonBatched();

		/// <inheritdoc />
		public abstract Task ExecuteNonBatchedAsync(CancellationToken cancellationToken);

		public void ProcessResults(IShardedQueryBatchImplementor queryBatch, int queryIndex)
		{
			var exitStrategy = BuildListExitStrategy();
			var operation = new GetResultShardOperation(queryBatch, queryIndex);
			var results = queryBatch.Session.Execute(operation, exitStrategy);
			ProcessFinalResults(results as IList<TSource> ?? new List<TSource>(results));
		}

		public async Task ProcessResultsAsync(IShardedQueryBatchImplementor queryBatch, int queryIndex, CancellationToken cancellationToken)
		{
			var exitStrategy = BuildListExitStrategy();
			var operation = new GetResultShardOperation(queryBatch, queryIndex);
			var results = await queryBatch.Session.ExecuteAsync(operation, exitStrategy, cancellationToken).ConfigureAwait(false);
			ProcessFinalResults(results as IList<TSource> ?? new List<TSource>(results));
		}

		#endregion

		#region Protected methods

		protected abstract IListExitStrategy<TSource> BuildListExitStrategy();

		protected void ProcessFinalResults(IList<TSource> results)
		{
			var transformedResults = TransformResults(results);
			this.AfterLoadCallback?.Invoke(transformedResults);
			this.finalResults = transformedResults;
		}

		protected abstract IList<TResult> TransformResults(IList<TSource> results);

		#endregion

		#region Inner classes

		private class GetResultShardOperation : IShardOperation<IEnumerable<TSource>>, IAsyncShardOperation<IEnumerable<TSource>>
		{
			private readonly IShardedQueryBatchImplementor shardedQueryBatch;
			private readonly int queryIndex;

			public GetResultShardOperation(IShardedQueryBatchImplementor shardedQueryBatch, int queryIndex)
			{
				this.shardedQueryBatch = shardedQueryBatch;
				this.queryIndex = queryIndex;
			}

			public Func<IEnumerable<TSource>> Prepare(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var multiQuery = this.shardedQueryBatch.EstablishFor(shard);
				return () => multiQuery.GetResult<TSource>(this.queryIndex);
			}

			public Func<CancellationToken, Task<IEnumerable<TSource>>> PrepareAsync(IShard shard)
			{
				var multiQuery = this.shardedQueryBatch.EstablishFor(shard);
				return async ct => await multiQuery.GetResultAsync<TSource>(this.queryIndex, ct).ConfigureAwait(false);
			}

			public string OperationName
			{
				get { return $"GetResult({this.queryIndex})"; }
			}
		}

		#endregion
	}

	internal abstract class ShardedBatchItem<TResult> : ShardedBatchItem<TResult, TResult>
	{
		protected override IList<TResult> TransformResults(IList<TResult> results)
		{
			return results;
		}
	}
}