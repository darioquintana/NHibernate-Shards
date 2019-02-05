using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Engine;
using NHibernate.Multi;
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

		/// <inheritdoc />
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

		/// <inheritdoc />
		public abstract void ProcessResults(IShardedQueryBatchImplementor queryBatch, int queryIndex);

		/// <inheritdoc />
		public abstract Task ProcessResultsAsync(IShardedQueryBatchImplementor queryBatch, int queryIndex, CancellationToken cancellationToken);

		#endregion

		#region Protected methods

		protected void ProcessFinalResults(IList<TSource> results)
		{
			var transformedResults = TransformResults(results);
			this.AfterLoadCallback?.Invoke(transformedResults);
			this.finalResults = transformedResults;
		}

		protected abstract IList<TResult> TransformResults(IList<TSource> results);

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