using System.Collections.Generic;
using NHibernate.Shards.LoadBalance;
using NHibernate.Shards.Strategy.Selection;
using NSubstitute;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Selection
{
	[TestFixture]
	public class LoadBalancedShardSelectionStrategyTest
	{
		[Test]
		public void TestSelectShardForNewObject()
		{
			IList<ShardId> shardIds = new List<ShardId>();
			ShardId shardId = new ShardId(1);

			IShardLoadBalancer balancer = Substitute.For<IShardLoadBalancer>();
		    balancer.NextShardId.Returns(shardId);

		    LoadBalancedShardSelectionStrategy strategy = new LoadBalancedShardSelectionStrategy(balancer);
			Assert.AreEqual(shardId, strategy.SelectShardIdForNewObject(null));
			Assert.AreEqual(shardId, strategy.SelectShardIdForNewObject(null));
		}
	}
}