using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetReadOnlyEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetReadOnlyEventReadOnly()
		{
			SetReadOnlyEvent eve = new SetReadOnlyEvent(true);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetReadOnly(true)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
