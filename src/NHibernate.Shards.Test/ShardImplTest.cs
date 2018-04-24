using NHibernate.Engine;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Session;
using NSubstitute;
using NUnit.Framework;

namespace NHibernate.Shards.Test
{
	[TestFixture]
	public class ShardImplTest
	{
		[Test]
		public void EstablishSessionReturnsNewSessionOnFirstCall()
		{
			var shardedSessionImplementor = Substitute.For<IShardedSessionImplementor>();
			var shard = new ShardImpl(
				shardedSessionImplementor,
				new ShardMetadataImpl(new ShardId(1), Substitute.For<ISessionFactoryImplementor>()));

            shardedSessionImplementor.EstablishFor(shard).Returns(Substitute.For<ISession>());

			var session = shard.EstablishSession();
            Assert.That(session, Is.Not.Null, "First establish creates new session");
		}

		[Test]
		public void EstablishSessionReturnsExistingSessionOnNextCall()
		{
			var shardedSessionImplementor = Substitute.For<IShardedSessionImplementor>();
			var shard = new ShardImpl(
				shardedSessionImplementor,
				new ShardMetadataImpl(new ShardId(1), Substitute.For<ISessionFactoryImplementor>()));

			shardedSessionImplementor.EstablishFor(shard).Returns(Substitute.For<ISession>());

			var session = shard.EstablishSession();
			var session2 = shard.EstablishSession();
			Assert.That(session2, Is.SameAs(session), "Next establish returns existing session");
		}
    }
}
