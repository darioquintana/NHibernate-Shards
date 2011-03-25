using System;
using System.Collections.Generic;
using System.Linq;
using Iesi.Collections.Generic;
using NHibernate.Engine;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Session;
using NHibernate.Stat;

namespace NHibernate.Shards.Stat
{
	internal class ShardedSessionStatistics : ISessionStatistics
	{
	    private Set<ISessionStatistics> sessionStats;

        public ShardedSessionStatistics(IShardedSessionImplementor session)
        {
            sessionStats = new HashedSet<ISessionStatistics>();
            foreach(IShard shard in session.Shards)
            {
                if(shard.Session != null)
                {
                    sessionStats.Add(shard.Session.Statistics);
                }
                else
                {
                    IOpenSessionEvent ose = new StatOpenSessionEvent(sessionStats);
                    shard.AddOpenSessionEvent(ose);
                }
            }
        }

		#region ISessionStatistics Members

		///<summary>
		/// Get the number of entity instances associated with the session
		///</summary>
		///
		public int EntityCount
		{
			get
			{
			    int count = 0;
                foreach(ISessionStatistics stats in sessionStats)
			    {
			        count += stats.EntityCount;
			    }
                return count;
			}
		}

		///<summary>
		/// Get the number of collection instances associated with the session
		///</summary>
		///
		public int CollectionCount
		{
			get
			{
			    int count = 0;
                foreach (ISessionStatistics stats in sessionStats)
                {
                    count += stats.CollectionCount;
                }
                return count;
			}
		}

		///<summary>
		/// Get the set of all <see cref="T:NHibernate.Engine.EntityKey">EntityKeys</see>.
		///</summary>
		///
		public IList<EntityKey> EntityKeys
		{
			get
			{
			    Set<EntityKey> entityKeys = new HashedSet<EntityKey>();
                foreach(ISessionStatistics stat in sessionStats)
                {
                    var shardEntityKeys = stat.EntityKeys;
                    entityKeys.AddAll(shardEntityKeys);
                }

			    return entityKeys.ToList();
			}
		}

		///<summary>
		/// Get the set of all <see cref="T:NHibernate.Engine.CollectionKey">CollectionKeys</see>.
		///</summary>
		///
		public IList<CollectionKey> CollectionKeys
		{
			get
			{
			    Set<CollectionKey> collectionKeys = new HashedSet<CollectionKey>();
                foreach(ISessionStatistics stats in sessionStats)
                {
                    var shardCollectionKeys = stats.CollectionKeys;
                    collectionKeys.AddAll(shardCollectionKeys);
                }
			    return collectionKeys.ToList();
			}            
		}

        private class StatOpenSessionEvent:IOpenSessionEvent
        {
            private Set<ISessionStatistics> sessionStats;

            public StatOpenSessionEvent(Set<ISessionStatistics> sessionStats)
            {
                this.sessionStats = sessionStats;
            }

            public void OnOpenSession(ISession session)
            {
                sessionStats.Add(session.Statistics);
            }
        }

		#endregion
	}
}