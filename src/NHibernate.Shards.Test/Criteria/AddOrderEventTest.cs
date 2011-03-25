using NHibernate.Shards.Criteria;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Criteria
{
	[TestFixture]
	public class AddOrderEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestOnOpenSession()
		{
			var @event = new AddOrderEvent(null);
			var crit = Mock<ICriteria>();

			using (Mocks.Record())
			{
				Expect.Call(crit.AddOrder(null)).Return(crit);
			}

			using (Mocks.Playback())
			{
				@event.OnEvent(crit);
			}
		}
	}
}