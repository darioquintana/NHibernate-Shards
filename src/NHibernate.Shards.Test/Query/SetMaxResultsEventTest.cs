using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetMaxResultsEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetMaxResultsEventMaxResults()
		{
			SetMaxResultsEvent eve = new SetMaxResultsEvent(-1);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetMaxResults(-1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
