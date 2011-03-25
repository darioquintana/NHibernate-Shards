using System;
using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetTimestampEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetTimestampEventPositionVal()
		{
			DateTime now = DateTime.Now;
			SetTimestampEvent eve = new SetTimestampEvent(-1, now);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetTimestamp(-1, now)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetTimestampEventNameVal()
		{
			DateTime now = DateTime.Now;
			SetTimestampEvent eve = new SetTimestampEvent(null, now);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetTimestamp(null, now)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
