using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetCacheableEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetCacheableEventCacheable()
		{
			SetCacheableEvent eve = new SetCacheableEvent(false);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetCacheable(false)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}