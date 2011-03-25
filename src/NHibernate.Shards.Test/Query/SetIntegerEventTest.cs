using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetIntegerEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetIntegerEventPositionVal()
		{
			SetIntegerEvent eve = new SetIntegerEvent(-1, 1);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetInt32(-1, 1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetIntegerEventNameVal()
		{
			SetIntegerEvent eve = new SetIntegerEvent(null, 1);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetInt32(null, 1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
