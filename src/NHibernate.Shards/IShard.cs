using System.Collections.Generic;

namespace NHibernate.Shards
{
    /// <summary>
    /// Interface representing a Shard.  A shard is a physical partition (as opposed
    /// to a virtual partition). Shards know how to lazily instantiate Sessions.
    /// </summary>
    public interface IShard
    {
        ISessionFactory SessionFactory { get; }

        /// <summary>
        /// Readonly set of the virtual shards that are mapped to this physical shard.
        /// </summary>
        IList<ShardId> ShardIds { get; }

        /// <summary>
        /// Opens new session for this shard.
        /// </summary>
        ISession EstablishSession();

        bool Contains(object entity);
    }
}