using System.Collections.Generic;
using Iesi.Collections.Generic;
using NHibernate.Engine;
using NHibernate.Shards.Strategy;

namespace NHibernate.Shards.Session
{
	public class SubsetShardedSessionFactoryImpl : ShardedSessionFactoryImpl
	{

		public SubsetShardedSessionFactoryImpl(IList<ShardId> shardIds,
			IDictionary<ISessionFactoryImplementor, Set<ShardId>> sessionFactoryShardIdMap,
			IShardStrategyFactory shardStrategyFactory,
			ISet<System.Type> classesWithoutTopLevelSaveSupport,
			bool checkAllAssociatedObjectsForDifferentShards)
			:
			base(shardIds, sessionFactoryShardIdMap, shardStrategyFactory,
				classesWithoutTopLevelSaveSupport,
				checkAllAssociatedObjectsForDifferentShards)
		{


		}

		protected SubsetShardedSessionFactoryImpl(
			IDictionary<ISessionFactoryImplementor, Set<ShardId>> sessionFactoryShardIdMap,
			IShardStrategyFactory shardStrategyFactory,
			ISet<System.Type> classesWithoutTopLevelSaveSupport,
			bool checkAllAssociatedObjectsForDifferentShards) :
			base(sessionFactoryShardIdMap, shardStrategyFactory,
			  classesWithoutTopLevelSaveSupport,
			  checkAllAssociatedObjectsForDifferentShards)
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
