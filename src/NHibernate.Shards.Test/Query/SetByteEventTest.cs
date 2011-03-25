using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetByteEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetByteEventPositionVal()
		{
			SetByteEvent eve = new SetByteEvent(-1, (byte)0);
			
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetByte(-1, (byte)0)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetByteEventNameVal()
		{
			SetByteEvent eve = new SetByteEvent(null, (byte)0);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetByte(null, (byte)0)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
