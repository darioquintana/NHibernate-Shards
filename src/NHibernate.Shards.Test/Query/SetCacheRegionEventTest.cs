using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetCacheRegionEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetCacheRegionEventCacheRegion()
		{
			SetCacheRegionEvent eve = new SetCacheRegionEvent(null);
			
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetCacheRegion(null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
		
	}
}
