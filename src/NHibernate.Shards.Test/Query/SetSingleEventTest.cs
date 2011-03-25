using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetSingleEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetSingleEventPositionVal()
		{
			SetSingleEvent eve = new SetSingleEvent(-1, -1f);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetSingle(-1, -1f)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetSingleEventNameVal()
		{
			SetSingleEvent eve = new SetSingleEvent(null, -1f);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetSingle(null, -1f)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
