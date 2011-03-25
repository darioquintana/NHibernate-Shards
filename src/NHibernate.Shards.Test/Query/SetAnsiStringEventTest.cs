using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetAnsiStringEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetAnsiStringEventPositionVal()
		{
			SetAnsiStringEvent eve = new SetAnsiStringEvent(-1, "");
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetAnsiString(-1, "")).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetAnsiStringEventNameVal()
		{
			SetAnsiStringEvent eve = new SetAnsiStringEvent(null, "");

			IQuery query = Mocks.Stub<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetAnsiString(null, "")).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
