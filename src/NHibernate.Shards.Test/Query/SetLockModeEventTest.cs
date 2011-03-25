using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetLockModeEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetLockModeEventLockMode()
		{
			SetLockModeEvent eve = new SetLockModeEvent(null, null);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetLockMode(null, null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
