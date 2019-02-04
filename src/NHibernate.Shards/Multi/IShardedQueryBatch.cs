using NHibernate.Multi;

namespace NHibernate.Shards.Multi
{
	public interface IShardedQueryBatch : IQueryBatch
	{
		/// <summary>
		/// Gets number of queries in his batch.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Returns an <see cref="IQueryBatch"/> instance that is associated with the
		/// established session for a given shard.
		/// </summary>
		/// <param name="shard">A shard.</param>
		/// <returns>An <see cref="IQueryBatch"/> instance that is associated with the
		/// established session for <paramref name="shard"/>.</returns>
		IQueryBatch EstablishFor(IShard shard);
	}
}