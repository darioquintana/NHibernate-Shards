namespace NHibernate.Shards.Query
{
    public interface IShardedMultiQuery: IMultiQuery
    {
        /// <summary>
        /// Returns an <see cref="IMultiQuery"/> instance that is associated with the
        /// established session for a given shard.
        /// </summary>
        /// <param name="shard">A shard.</param>
        /// <returns>An <see cref="IMultiQuery"/> instance that is associated with the
        /// established session for <paramref name="shard"/>.</returns>
        IMultiQuery EstablishFor(IShard shard);
    }
}
