using System.Collections.Generic;
using Iesi.Collections.Generic;
using NHibernate.Shards.Session;
using NHibernate.Shards.Strategy;
using NHibernate.Engine;

namespace NHibernate.Shards.Criteria
{
	/**
	 * This class extends ShardedSessionFactoryImpl and is constructed by supplying
	 * a subset of shardIds that are primarily owned by a ShardedSessionFactoryImpl.
	 * The purpose of this class is to override the .close() method in order to
	 * prevent the SubsetShardedSessionFactoryImpl from closing any session
	 * factories that belong to a ShardedSessionFactoryImpl.
	 *
	 * @author Maulik Shah@google.com (Maulik Shah)
	 */
    public class SubsetShardedSessionFactoryImpl : ShardedSessionFactoryImpl
    {
        public SubsetShardedSessionFactoryImpl(ICollection<ShardId> shardIds,
                                               IDictionary<ISessionFactoryImplementor,Set<ShardId>> sessionFactoryShardIdMap,
                                               IShardStrategyFactory shardStrategyFactory,
                                               Set<System.Type> classesWithoutTopLevelSaveSupport,
                                               bool checkAllAssociatedObjectsForDifferentShards):base(shardIds,sessionFactoryShardIdMap,shardStrategyFactory,classesWithoutTopLevelSaveSupport,checkAllAssociatedObjectsForDifferentShards)
        {
            
        }

		/**
		 * This method is a NO-OP. As a ShardedSessionFactoryImpl that represents
		 * a subset of the application's shards, it will not close any shard's
		 * sessionFactory.
		 *
		 * @throws HibernateException
		 */
		public override void Close()
		{
			// no-op: this class should never close session factories
		}

    }
}
