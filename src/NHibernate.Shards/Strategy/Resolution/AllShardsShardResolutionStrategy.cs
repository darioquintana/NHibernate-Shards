using System.Collections.Generic;
using NHibernate.Shards.Engine;

namespace NHibernate.Shards.Strategy.Resolution
{
    public class AllShardsShardResolutionStrategy : BaseShardResolutionStrategy
    {
        public AllShardsShardResolutionStrategy(IEnumerable<ShardId> shardIds)
            : base(shardIds)
        { }

        public override IEnumerable<ShardId> ResolveShardIds(ShardedEntityKey id)
        {
            return ShardIds;
        }
    }
}