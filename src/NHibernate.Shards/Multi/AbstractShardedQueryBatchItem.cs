namespace NHibernate.Shards.Multi
{
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

	internal abstract class AbstractShardedQueryBatchItem<T> : IShardedQueryBatchItemImplementor<T>
	{
		#region Instance fields

		private IList<T> finalResults;

		#endregion

		#region IQueryBatchItem implementation

		/// <inheritdoc />
		Task<int> IQueryBatchItem.ProcessResultsSetAsync(DbDataReader reader, CancellationToken cancellationToken)
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		public abstract Task ExecuteNonBatchedAsync(CancellationToken cancellationToken);

		/// <inheritdoc />
		void IQueryBatchItem.Init(ISessionImplementor session)
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

		/// <inheritdoc />
		int IQueryBatchItem.ProcessResultsSet(DbDataReader reader)
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		void IQueryBatchItem.ProcessResults()
		{
			throw NotSupportedException();
		}

		/// <inheritdoc />
		public abstract void ExecuteNonBatched();

		/// <inheritdoc />
		IEnumerable<ICachingInformation> IQueryBatchItem.CachingInformation
		{
			get { throw NotSupportedException(); }
		}

		private Exception NotSupportedException([CallerMemberName]string operationName = null)
		{
			throw new NotSupportedException($"{nameof(IQueryBatchItem)}.{operationName} is not supported on sharded query batches");
		}

		#endregion

		#region IQueryBatchItem<T> implementation

		/// <inheritdoc />
		public IList<T> GetResults()
		{
			return this.finalResults;
		}

		/// <inheritdoc />
		public Action<IList<T>> AfterLoadCallback { get; set; }

		#endregion

		#region IShardedQueryBatchItem implementation

		public bool HasResults
		{
			get { return this.finalResults != null; }
		}

		/// <inheritdoc />
		public abstract IExitOperationFactory ExitOperationFactory { get; }

		/// <inheritdoc />
		public abstract void EstablishFor(IShard shard, IQueryBatch queryBatch);

		/// <inheritdoc />
		public abstract void EstablishFor(IShard shard, string key, IQueryBatch queryBatch);

		#endregion

		#region IShardedQueryBatchItemImplementor<T> implementation

		public void ProcessResults(IList<T> results)
		{
			this.AfterLoadCallback?.Invoke(results);
			this.finalResults = results;
		}

		#endregion
	}
}