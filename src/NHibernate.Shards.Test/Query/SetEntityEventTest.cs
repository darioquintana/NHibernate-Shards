using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetEntityEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetEntityEventPositionVal()
		{
			SetEntityEvent eve = new SetEntityEvent(-1, null);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetEntity(-1, null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetEntityEventNameVal()
		{
			SetEntityEvent eve = new SetEntityEvent(null, null);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetEntity(null, null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
