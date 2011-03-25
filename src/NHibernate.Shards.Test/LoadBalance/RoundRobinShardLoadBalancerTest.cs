using System.Collections.Generic;
using NHibernate.Shards.LoadBalance;
using NUnit.Framework;

namespace NHibernate.Shards.Test.LoadBalance
{
	[TestFixture]
	public class RoundRobinShardLoadBalancerTest
	{
		[Test]
		public void TestBalancer()
		{
			List<ShardId> shardIds = new List<ShardId> {new ShardId(1), new ShardId(2)};
			RoundRobinShardLoadBalancer balancer = new RoundRobinShardLoadBalancer(shardIds);
			Assert.AreEqual(1, balancer.NextShardId.Id);
			Assert.AreEqual(2, balancer.NextShardId.Id);
			Assert.AreEqual(1, balancer.NextShardId.Id);
		}
	}
}