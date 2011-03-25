using NHibernate.Shards.Query;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;
using Rhino.Mocks;

namespace NHibernate.Shards.Test.Query
{
	[TestFixture]
	public class SetCharacterEventTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestSetCharacterEventPositionVal()
		{
			SetCharacterEvent eve = new SetCharacterEvent(-1, 'a');
			
			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetCharacter(-1, 'a')).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}

		[Test]
		public void TestSetCharacterEventNameVal()
		{
			SetCharacterEvent eve = new SetCharacterEvent(null, 'a');

			IQuery query = Mock<IQuery>();
			using (Mocks.Record())
			{
				Expect.Call(query.SetCharacter(null, 'a')).Return(query);
			}

			using (Mocks.Playback())
			{
				eve.OnEvent(query);
			}
		}
		
	}
}
