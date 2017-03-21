namespace NHibernate.Shards.Criteria
{
    using System.Collections.Generic;
    using NHibernate.Criterion;
    using NHibernate.Impl;
    using NHibernate.Shards.Engine;

    public class ShardedQueryOver<TRoot, TSubType> : QueryOver<TRoot, TSubType>, IQueryOver<TRoot>
    {
        private IShardedCriteria shardedCriteria;

        protected internal ShardedQueryOver(IShardedCriteria shardedCriteria, IShardedSessionImplementor shardedSession)
            : base((CriteriaImpl)shardedCriteria.EstablishFor(shardedSession.AnyShard), shardedCriteria)
        {
            this.shardedCriteria = shardedCriteria;
        }

    }
}
