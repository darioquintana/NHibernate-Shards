using NHibernate.Shards.Criteria;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Criteria
{
    [TestFixture]
    public class AddCriterionEventTest : TestFixtureBaseWithMock
    {
        [Test, Ignore]
        public void TestOnOpenSession()
        {
            var @event = new AddCriterionEvent(null);
            bool[] called = { false };
            var crit = Mock<ICriteria>(); 

            using (Mocks.Record())
            {
                Expect
                      .On(@event)
                      .Call(crit)//.Do(called[0] = true)
                      .Return(null);
            }

            using (Mocks.Playback())
            {
                Assert.Equals(called[0], true);
            }
        }
    }
}
/*
public Criteria add(Criterion criterion) {
        called[0] = true;
        return null;
      } 
    };
    event.onEvent(crit);*/