using System.Collections.Generic;
using NHibernate.Shards.Engine;

namespace NHibernate.Shards.Strategy.Resolution
{
    public abstract class BaseShardResolutionStrategy : BaseHasShardIdList, IShardResolutionStrategy
    {
        protected BaseShardResolutionStrategy(IEnumerable<ShardId> shardIds)
            : base(shardIds)
        { }

        public abstract IEnumerable<ShardId> ResolveShardIds(ShardedEntityKey id);
    }
}