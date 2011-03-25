using NHibernate.Mapping;
using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetPropertiesEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetPropertiesEventBean()
		{
			SetPropertiesEvent eve = new SetPropertiesEvent(null);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetProperties(null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetLongEventMap()
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
