using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetResultTransformerEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetResultTransformerEventResultTransformer()
		{
			SetResultTransformerEvent eve = new SetResultTransformerEvent(null);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetResultTransformer(null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
