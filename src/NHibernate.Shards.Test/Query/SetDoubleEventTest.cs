using System;
using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetDoubleEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetBinaryEventPositionVal()
		{
			SetDoubleEvent eve = new SetDoubleEvent(-1, -1.0d);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetDouble(-1, -1.0d)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetBinaryEventNameVal()
		{
			SetDoubleEvent eve = new SetDoubleEvent(null, -1.0d);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetDouble(null, -1.0d)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
