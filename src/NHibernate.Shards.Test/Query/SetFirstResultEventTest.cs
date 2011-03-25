using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetFirstResultEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetFirstResultEventFirstResult()
		{
			SetFirstResultEvent eve = new SetFirstResultEvent(-1);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetFirstResult(-1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
