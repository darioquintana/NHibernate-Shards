using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetDecimalEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetDateTimeEventPositionVal()
		{
			SetDecimalEvent eve = new SetDecimalEvent(-1, -1 );
			
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetDecimal(-1, -1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetDecimalEventNameVal()
		{
			SetDecimalEvent eve = new SetDecimalEvent(null, -1);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetDecimal(null, -1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
