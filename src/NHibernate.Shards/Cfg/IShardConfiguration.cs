using NHibernate.Shards.Session;

namespace NHibernate.Shards.Cfg
{
    /// <summary>
    /// Describes the configuration properties that can vary across the <see cref="ISessionFactory"/>
    /// instances contained within your <see cref="IShardedSessionFactory"/>.
    /// </summary>
    public interface IShardConfiguration
    {
        string DefaultSchema { get; }

        /// <summary>
        /// the name that the <see cref="ISessionFactory"/> created from this config will have
        /// </summary>
        string ShardSessionFactoryName { get; }

        /// <summary>
        /// unique id of the shard
        /// </summary>
        int ShardId { get; }

        /// <summary>
        /// the cache region prefix for the shard
        /// </summary>
        string ShardCacheRegionPrefix { get; }

        /// <summary>
        /// Connection string of the shard.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Named connection string of the shard.
        /// </summary>
        string ConnectionStringName { get; }
    }
}