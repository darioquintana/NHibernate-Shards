using System;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Strategy.Access;
using NHibernate.Shards.Strategy.Resolution;
using NHibernate.Shards.Strategy.Selection;

namespace NHibernate.Shards.Test
{
    public class ShardStrategyDefaultMock: IShardStrategy
    {
        public virtual IShardSelectionStrategy ShardSelectionStrategy
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IShardResolutionStrategy ShardResolutionStrategy
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IShardAccessStrategy ShardAccessStrategy
        {
            get { throw new NotSupportedException(); }
        }
    }
}
