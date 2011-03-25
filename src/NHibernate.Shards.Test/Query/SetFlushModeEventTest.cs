using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetFlushModeEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetFlushModeEventFlushMode()
		{
			SetFlushModeEvent eve = new SetFlushModeEvent(FlushMode.Unspecified);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetFlushMode(FlushMode.Unspecified)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
