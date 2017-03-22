namespace NHibernate.Shards.Criteria
{
    using System;
    using NHibernate.Criterion;
    using NHibernate.Engine;
    using NHibernate.Impl;

    public class ShardedQueryOver<TRoot> : QueryOver<TRoot, TRoot>, IQueryOver<TRoot>, ICloneable
    {
        protected internal ShardedQueryOver(ShardedCriteriaImpl shardedCriteria)
            : base((CriteriaImpl)shardedCriteria.SomeCriteria, shardedCriteria)
        {}

        protected internal ShardedQueryOver(ShardedQueryOver<TRoot> other)
            : this((ShardedCriteriaImpl)other.ShardedCriteria.Clone())
        { }

        private IShardedCriteria ShardedCriteria
        {
            get { return (IShardedCriteria)this.criteria; }
        }

        public int RowCount()
        {
            return ToRowCountQuery().SingleOrDefault<int>();
        }

        public long RowCountInt64()
        {
            return ToRowCountInt64Query().SingleOrDefault<long>();
        }

        /// <summary>
        /// Creates an exact clone of the QueryOver
        /// </summary>
        public new QueryOver<TRoot, TRoot> Clone()
        {
            return new ShardedQueryOver<TRoot>(this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Clones the QueryOver, clears the orders and paging, and projects the RowCount
        /// </summary>
        /// <returns></returns>
        public new IQueryOver<TRoot, TRoot> ToRowCountQuery()
        {
            return (IQueryOver<TRoot, TRoot>)Clone()
                .Select(Projections.RowCount())
                .ClearOrders()
                .Skip(0)
                .Take(RowSelection.NoValue);
        }

        /// <summary>
        /// Clones the QueryOver, clears the orders and paging, and projects the RowCount (Int64)
        /// </summary>
        /// <returns></returns>
        public new IQueryOver<TRoot, TRoot> ToRowCountInt64Query()
        {
            return (IQueryOver<TRoot, TRoot>)Clone()
                .Select(Projections.RowCountInt64())
                .ClearOrders()
                .Skip(0)
                .Take(RowSelection.NoValue);
        }
    }
}
