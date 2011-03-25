using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetFetchSizeEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetFetchSizeEventFetchSize()
		{
			SetFetchSizeEvent eve = new SetFetchSizeEvent(-1);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetFetchSize(-1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
