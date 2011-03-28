using System.Collections.Generic;
using NHibernate.Engine;

namespace NHibernate.Shards.Engine
{
    /// <summary>
    /// Represents metadata for one shard.
    /// </summary>
    public interface IShardMetadata
    {
        /// <summary>
        /// All shard identifiers for the shard.
        /// </summary>
        IEnumerable<ShardId> ShardIds { get; }

        /// <summary>
        /// The session factory for the shard.
        /// </summary>
        ISessionFactoryImplementor SessionFactory { get; }
    }
}
