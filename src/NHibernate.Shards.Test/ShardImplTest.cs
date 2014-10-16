using NHibernate.Engine;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Session;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test
{
	[TestFixture]
	public class ShardImplTest : TestFixtureBaseWithMock
	{
		[Test]
		public void EstablishSessionReturnsNewSessionOnFirstCall()
		{
			var shardedSessionImplementor = Stub<IShardedSessionImplementor>();
			var shard = new ShardImpl(
				shardedSessionImplementor,
				new ShardMetadataImpl(new ShardId(1), Stub<ISessionFactoryImplementor>()));

			shardedSessionImplementor.Expect(x => x.EstablishFor(shard)).Return(Stub<ISession>());

			var session = shard.EstablishSession();
			shardedSessionImplementor.VerifyAllExpectations();
			Assert.That(session, Is.Not.Null, "First establish creates new session");
		}

		[Test]
		public void EstablishSessionReturnsExistingSessionOnNextCall()
		{
			var shardedSessionImplementor = Stub<IShardedSessionImplementor>();
			var shard = new ShardImpl(
				shardedSessionImplementor,
				new ShardMetadataImpl(new ShardId(1), Stub<ISessionFactoryImplementor>()));

			shardedSessionImplementor.Expect(x => x.EstablishFor(shard)).Return(Stub<ISession>());

			var session = shard.EstablishSession();
			var session2 = shard.EstablishSession();
			Assert.That(session2, Is.SameAs(session), "Next establish returns existing session");

			shardedSessionImplementor.VerifyAllExpectations();
		}
	}
}
