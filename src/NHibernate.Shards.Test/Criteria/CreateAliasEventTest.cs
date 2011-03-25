using NHibernate.Shards.Criteria;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Criteria
{
	[TestFixture]
	public class CreateAliasEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestOnOpenSession()
		{
			CreateAliasEvent eve = new CreateAliasEvent(null, null);
			ICriteria crit = Mock<ICriteria>();

			using (Mocks.Record())
			{
				Expect.Call(crit.CreateAlias(null, null)).Return(crit);
			}
			using (Mocks.Playback())
			{
				eve.OnEvent(crit);
			}
		}

		[Test]
		public void TestOnOpenSessionWithJoinType()
		{
			CreateAliasEvent eve = new CreateAliasEvent(null, null, 0);
			ICriteria crit = Mock<ICriteria>();

			using (Mocks.Record())
			{
				Expect.Call(crit.CreateAlias(null, null, 0)).Return(crit);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(crit);
			}
		}
	}
}