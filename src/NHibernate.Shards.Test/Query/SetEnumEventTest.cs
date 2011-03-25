using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetEnumEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetEnumEventPositionVal()
		{
			SetEnumEvent eve = new SetEnumEvent(-1, null);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetEnum(-1, null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetEnumEventNameVal()
		{
			SetEnumEvent eve = new SetEnumEvent(null, null);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetEnum(null, null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
