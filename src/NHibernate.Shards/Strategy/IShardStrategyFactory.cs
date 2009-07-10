using System.Collections.Generic;

namespace NHibernate.Shards.Strategy
{
	public interface IShardStrategyFactory
	{
		IShardStrategy NewShardStrategy(ICollection<ShardId> shardIds);
	}
}