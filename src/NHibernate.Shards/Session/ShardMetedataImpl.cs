using System.Collections.Generic;
using NHibernate.Engine;
using NHibernate.Shards.Engine;

namespace NHibernate.Shards.Session
{
    public class ShardMetadataImpl: IShardMetadata
    {
        private readonly IEnumerable<ShardId> shardIds;
        private readonly ISessionFactoryImplementor sessionFactory;

        public ShardMetadataImpl(ShardId shardId, ISessionFactoryImplementor sessionFactory)
            : this(new[] { shardId }, sessionFactory)
        {}

        public ShardMetadataImpl(IEnumerable<ShardId> shardIds, ISessionFactoryImplementor sessionFactory)
        {
            this.shardIds = shardIds;
            this.sessionFactory = sessionFactory;
        }

        public IEnumerable<ShardId> ShardIds
        {
            get { return this.shardIds; }
        }
        
        public ISessionFactoryImplementor SessionFactory
        {
            get { return this.sessionFactory; }
        }
    }
}
