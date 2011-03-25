using System.Collections.Generic;
using NHibernate.Shards.LoadBalance;
using NHibernate.Shards.Strategy.Selection;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Strategy.Selection
{
	[TestFixture]
	public class LoadBalancedShardSelectionStrategyTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSelectShardForNewObject()
		{
			IList<ShardId> shardIds = new List<ShardId>();
			ShardId shardId = new ShardId(1);

			IShardLoadBalancer balancer = Mock<IShardLoadBalancer>();

			using (Mocks.Record())
			{
				Expect.Call(balancer.NextShardId).Return(shardId);
				Expect.Call(balancer.NextShardId).Return(shardId);
			}

			using (Mocks.Playback())
			{
				LoadBalancedShardSelectionStrategy strategy = new LoadBalancedShardSelectionStrategy(balancer);
				Assert.AreEqual(shardId, strategy.SelectShardIdForNewObject(null));
				Assert.AreEqual(shardId, strategy.SelectShardIdForNewObject(null));
			}
		}
	}
}