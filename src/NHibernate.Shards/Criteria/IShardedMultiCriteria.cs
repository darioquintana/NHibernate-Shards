namespace NHibernate.Shards.Criteria
{
    public interface IShardedMultiCriteria: IMultiCriteria
    {
        /// <summary>
        /// Returns an <see cref="IMultiCriteria"/> instance that is associated with the
        /// established session for a given shard.
        /// </summary>
        /// <param name="shard">A shard.</param>
        /// <returns>An <see cref="IMultiCriteria"/> instance that is associated with the
        /// established session for <paramref name="shard"/>.</returns>
        IMultiCriteria EstablishFor(IShard shard);
    }
}
