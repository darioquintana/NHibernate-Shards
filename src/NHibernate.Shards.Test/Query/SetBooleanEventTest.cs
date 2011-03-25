using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetBooleanEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetBooleanEventPositionVal()
		{
			SetBooleanEvent eve = new SetBooleanEvent(-1, false);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetBoolean(-1, false)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetBooleanEventNameVal()
		{
			SetBooleanEvent eve = new SetBooleanEvent(null, false);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetBoolean(null, false)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
		
	}
}
