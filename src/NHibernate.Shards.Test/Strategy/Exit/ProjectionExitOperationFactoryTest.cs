using System;
using NHibernate.Criterion;
using NHibernate.Engine;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{
	[TestFixture,Ignore]
	public class ProjectionExitOperationFactoryTest : TestFixtureBaseWithMock
	{
		[Test]
		public void TestReturnedOperations()
		{
			ProjectionExitOperationFactory factory = ProjectionExitOperationFactory.GetFactory();
			var sessionFactory = Mock<ISessionFactoryImplementor>();
			using (Mocks.Record())
			{
			}
			using (Mocks.Playback())
			{
				Assert.IsInstanceOf(typeof (RowCountExitOperation),
				                    factory.GetProjectionExitOperation(Projections.RowCount(), sessionFactory));
				Assert.IsInstanceOf(typeof (AggregateExitOperation),
				                    factory.GetProjectionExitOperation(Projections.Max("foo"), sessionFactory));
				Assert.IsInstanceOf(typeof (AggregateExitOperation),
				                    factory.GetProjectionExitOperation(Projections.Min("foo"), sessionFactory));
				Assert.IsInstanceOf(typeof (AggregateExitOperation),
				                    factory.GetProjectionExitOperation(Projections.Sum("foo"), sessionFactory));
				try
				{
					factory.GetProjectionExitOperation(Projections.Avg("foo"), sessionFactory);
					Assert.Fail("example of one that we don't yet support");
				}
				catch (ArgumentException e)
				{
					// good
				}
			}
		}
	}
}
