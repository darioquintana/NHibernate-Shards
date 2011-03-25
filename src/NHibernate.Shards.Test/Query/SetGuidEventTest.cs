using System;
using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetGuidEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetGuidEventPositionVal()
		{
			SetGuidEvent eve = new SetGuidEvent(-1, Guid.Empty);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetGuid(-1, Guid.Empty)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetGuidEventNameVal()
		{
			SetGuidEvent eve = new SetGuidEvent(null, Guid.Empty);

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetGuid(null, Guid.Empty)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
