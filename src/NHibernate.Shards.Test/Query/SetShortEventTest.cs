using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetShortEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetShortEventPositionVal()
		{
			SetShortEvent eve = new SetShortEvent(-1, (short)1);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetInt16(-1, (short)1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetShortEventNameVal()
		{
			SetShortEvent eve = new SetShortEvent(null, (short)1);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetInt16(null, (short)1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
