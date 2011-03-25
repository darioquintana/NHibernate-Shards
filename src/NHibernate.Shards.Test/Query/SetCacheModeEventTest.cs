using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetCacheModeEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetCacheModeEventCacheMode()
		{
			SetCacheModeEvent eve = new SetCacheModeEvent(CacheMode.Normal);
			
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetCacheMode(CacheMode.Normal)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
