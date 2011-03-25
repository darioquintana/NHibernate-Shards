using System.Collections;
using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetParameterListEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetParameterListEventNameValsColType()
		{
			SetParameterListEvent eve = new SetParameterListEvent(null, new ArrayList(), null);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetParameterList(null, new ArrayList(), null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetParameterListEventNameValsCol()
		{
			SetParameterListEvent eve = new SetParameterListEvent(null, new ArrayList());
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetParameterList(null, new ArrayList())).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}


		[Test]
		public void TestSetParameterListEventNameValsArrType()
		{
			SetParameterListEvent eve = new SetParameterListEvent(null, new object[0], null);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetParameterList(null, new object[0], null)).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetParameterListEventNameValsArr()
		{
			SetParameterListEvent eve = new SetParameterListEvent(null, new object[0]);
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetParameterList(null, new object[0])).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
	}
}
