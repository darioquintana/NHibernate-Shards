using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetBinaryEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetDoubleEventPositionVal()
		{
			SetBinaryEvent eve = new SetBinaryEvent(-1, new byte[]{0x0});
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetBinary(-1, new byte[]{0x0})).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetDoubleEventNameVal()
		{
			SetBinaryEvent eve = new SetBinaryEvent(null, new byte[]{0x0});

			IQuery query = Mocks.Stub<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetBinary(null, new byte[]{0x0})).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
		
	}
}
