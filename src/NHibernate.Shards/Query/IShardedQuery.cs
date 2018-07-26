namespace NHibernate.Shards.Query
{
    using NHibernate.Shards.Strategy.Exit;

    /// <summary>
    /// <see cref="IShardedQuery"/> extends the <see cref="IQuery"/> interface to 
    /// provide the ability to query across shards.
    /// </summary>
    public interface IShardedQuery : IQuery, IExitOperationFactory
	{
		/// <summary>
		/// Returns an <see cref="IQuery"/> instance that is associated with the
		/// established session of a given shard.
		/// </summary>
		/// <param name="shard">The shard for which an <see cref="IQuery"/> is to be established.</param>
		/// <returns>An <see cref="IQuery"/> instance that is associated with the established session 
		/// of <paramref name="shard"/>.
		/// </returns>
		IQuery EstablishFor(IShard shard);
	}
}