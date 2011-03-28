namespace NHibernate.Shards
{
    /// <summary>
    /// Simple interface to represent a shard-aware <see cref="ITransaction"/> 
    /// </summary>
    public interface IShardedTransaction : ITransaction
    {
        /// <summary>
        /// Enlists a shard-local session into the sharded transaction.
        /// </summary>
        /// <param name="session">The shard-local session to be enlisted.</param>
        void Enlist(ISession session);
    }
}