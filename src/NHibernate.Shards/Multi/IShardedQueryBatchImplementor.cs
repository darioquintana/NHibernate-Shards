using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Multi
{
	public interface IShardedQueryBatchImplementor
	{
		/// <summary>
		/// Collects and aggregates query results from unsharded query batches.
		/// </summary>
		/// <typeparam name="T">The result element type.</typeparam>
		/// <param name="queryIndex">Index of the query within query batch for which results are to collected.</param>
		/// <param name="exitStrategy">The strategy to be used for post-processing of unsharded query results.</param>
		/// <returns>The result of query with the given <paramref name="queryIndex"/>.</returns>
		IList<T> GetResults<T>(int queryIndex, IListExitStrategy<T> exitStrategy);

		/// <summary>
		/// Collects and aggregates query results from unsharded query batches.
		/// </summary>
		/// <typeparam name="T">The result element type.</typeparam>
		/// <param name="queryIndex">Index of the query within query batch for which results are to collected.</param>
		/// <param name="exitStrategy">The strategy to be used for post-processing of unsharded query results.</param>
		/// <param name="cancellationToken">A cancellation token for this asynchronous operation.</param>
		/// <returns>The result of query with the given <paramref name="queryIndex"/>.</returns>
		Task<IList<T>> GetResultsAsync<T>(int queryIndex, IListExitStrategy<T> exitStrategy, CancellationToken cancellationToken);
	}
}