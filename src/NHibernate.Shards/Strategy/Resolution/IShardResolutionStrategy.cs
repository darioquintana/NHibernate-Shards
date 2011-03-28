using System.Collections.Generic;
using NHibernate.Shards.Engine;

namespace NHibernate.Shards.Strategy.Resolution
{
    public interface IShardResolutionStrategy
    {
        IEnumerable<ShardId> ResolveShardIds(ShardedEntityKey key);
    }
}