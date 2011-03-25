using System;
using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetBigIntegerEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetBigIntegerEventPositionVal()
		{
			SetBigIntegerEvent eve = new SetBigIntegerEvent(-1, (Int64)1);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetInt64(-1, (Int64)1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetBigIntegerEventNameVal()
		{
			SetBigIntegerEvent eve = new SetBigIntegerEvent(null, (Int64)1);

			IQuery query = Mocks.Stub<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetInt64(null, (Int64)1)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
		
	}
}
