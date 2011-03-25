using NHibernate.Shards.Criteria;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Criteria
{
	[TestFixture]
	public class AddCriterionEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestOnOpenSession()
		{
			var @event = new AddCriterionEvent(null);
			var crit = Mock<ICriteria>();

			using (Mocks.Record())
			{
				Expect.Call(crit.Add(null)).Return(crit);
			}

			using (Mocks.Playback())
			{
				@event.OnEvent(crit);
			}
		}
	}
}