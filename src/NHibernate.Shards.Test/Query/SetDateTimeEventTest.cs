using System;
using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetDateTimeEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetDateTimeEventPositionVal()
		{
			DateTime now = DateTime.Now;
			SetDateTimeEvent eve = new SetDateTimeEvent(-1, now);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetDateTime(-1, now)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetDateTimeEventNameVal()
		{
			DateTime now = DateTime.Now;
			SetDateTimeEvent eve = new SetDateTimeEvent(null, now);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetDateTime(null, now)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
		
	}
}
