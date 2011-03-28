using System.Collections.Generic;
using System.Linq;
using NHibernate.Engine;
using NHibernate.Stat;

namespace NHibernate.Shards.Stat
{
    internal class ShardedSessionStatistics : ISessionStatistics
    {
        private readonly HashSet<ISessionStatistics> sessionStats = new HashSet<ISessionStatistics>();

        public void CollectFor(ISession session)
        {
            this.sessionStats.Add(session.Statistics);
        }

        public void Clear()
        {
            sessionStats.Clear();
        }

        #region ISessionStatistics Members

        ///<summary>
        /// Get the number of entity instances associated with the session
        ///</summary>
        public int EntityCount
        {
            get
            {
                int count = 0;
                foreach (ISessionStatistics stats in sessionStats)
                {
                    count += stats.EntityCount;
                }
                return count;
            }
        }

        ///<summary>
        /// Get the number of collection instances associated with the session
        ///</summary>
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
                return sessionStats
                    .SelectMany(stats => stats.EntityKeys)
                    .Distinct()
                    .ToArray();
            }
        }

        ///<summary>
        /// Get the set of all <see cref="T:NHibernate.Engine.CollectionKey">CollectionKeys</see>.
        ///</summary>
        public IList<CollectionKey> CollectionKeys
        {
            get
            {
                return sessionStats
                    .SelectMany(stats => stats.CollectionKeys)
                    .Distinct()
                    .ToArray();
            }
        }

        #endregion
    }
}