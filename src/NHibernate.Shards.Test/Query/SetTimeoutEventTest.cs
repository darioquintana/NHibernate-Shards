using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetTimeoutEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetTimeoutEventTimeout()
		{
			SetTimeoutEvent eve = new SetTimeoutEvent(-1);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetTimeout(-1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
