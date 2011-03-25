using System;
using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class AddTimeEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetTimeEventPositionVal()
		{
			DateTime now = DateTime.Now;
			SetTimeEvent eve = new SetTimeEvent(-1, now);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetTime(-1, now)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetTimeEventNameVal()
		{
			DateTime now = DateTime.Now;
			SetTimeEvent eve = new SetTimeEvent(null, now);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetTime(null, now)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}

}
