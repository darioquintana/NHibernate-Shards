using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetParameterEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetEntityEventPositionVal()
		{
			SetParameterEvent eve = new SetParameterEvent(-1, null);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetParameter(-1, null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetParameterEventNameVal()
		{
			SetParameterEvent eve = new SetParameterEvent(null, null);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetParameter(null, null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
