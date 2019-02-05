using NHibernate.Multi;

namespace NHibernate.Shards.Multi
{
	public interface IShardedQueryBatch : IQueryBatch
	{
		/// <summary>
		/// Gets number of queries in his batch.
		/// </summary>
		int Count { get; }
	}
}