using NHibernate.Shards.Criteria;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Criteria
{
	[TestFixture]
	public class AddOrderEventTest : TestFixtureBaseWithMock
	{
		[Test, Ignore]
		public void TestOnOpenSession()
		{
			var @event = new AddOrderEvent(null);
			bool[] called = {false};
			var crit = Mock<ICriteria>();

			using (Mocks.Record())
			{
				//Expect.Call(crit).Do(called[0]=true).Return(null);
			}

			using (Mocks.Playback())
			{
				@event.OnEvent(crit);
				Assert.Equals(called[0], true);
			}
			/*
                AddOrderEvent event = new AddOrderEvent(null);
                final boolean[] called = {false};
                Criteria crit = new CriteriaDefaultMock() {
                  @Override
                  public Criteria addOrder(Order order) {
                    called[0] = true;
                    return null;
                  }
                };
                event.onEvent(crit);
                assertTrue(called[0]);
                         */
		}
	}
}