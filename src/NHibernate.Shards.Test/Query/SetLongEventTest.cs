using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetLongEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetLongEventPositionVal()
		{
			SetLongEvent eve = new SetLongEvent(-1, (long)1);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetInt64(-1, (long)1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetLongEventNameVal()
		{
			SetLongEvent eve = new SetLongEvent(null, (long)1);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetInt64(null, (long)1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
