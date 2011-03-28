using System.Collections.Generic;
using NHibernate.Shards.Engine;

namespace NHibernate.Shards
{
    public class ShardImpl : IShard
    {
        private readonly IShardedSessionImplementor shardedSession;

        // ids of virtual shards mapped to this physical shard
        private readonly HashSet<ShardId> shardIds;

        // the SessionFactory that owns this Session
        private readonly ISessionFactory sessionFactory;

        private ISession session;

        public ShardImpl(IShardedSessionImplementor shardedSession, IShardMetadata shardMetadata)
        {
            // make a copy to be safe
            this.shardedSession = shardedSession;
            this.shardIds = new HashSet<ShardId>(shardMetadata.ShardIds); //TODO:make it a readonly Set
            this.sessionFactory = shardMetadata.SessionFactory;
        }

        /// <summary>
        /// SessionFactoryImplementor that owns the Session associated with this Shard
        /// </summary>
        public ISessionFactory SessionFactory
        {
            get { return this.sessionFactory; }
        }

        public ISession Session
        {
            get { return this.session; }
        }

        /// <summary>
        /// Ids of the virtual shards that are mapped to this physical shard.
        /// The returned Set is unmodifiable.
        /// </summary>
        public ICollection<ShardId> ShardIds
        {
            get { return this.shardIds; }
        }

        public bool Contains(object entity)
        {
            return this.session != null
                && this.session.Contains(entity);
        }

        public ISession EstablishSession()
        {
            if (this.session == null)
            {
                this.session = this.shardedSession.EstablishFor(this);
            }
            return this.session;
        }
    }
}