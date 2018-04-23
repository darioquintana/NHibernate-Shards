namespace NHibernate.Shards.Test.Session
{
    using NUnit.Framework;

    [TestFixture]
    public class SharedSessionFactoryImplTests: ShardedTestCase
    {
        [Test]
        public void CanCreateSessionUsingBuilder()
        {
            var sessionBuilder = this.SessionFactory.WithOptions();
            Assert.That(sessionBuilder, Is.Not.Null, nameof(this.SessionFactory.WithOptions));

            Assert.That(sessionBuilder.AutoClose(true), 
                Is.SameAs(sessionBuilder), nameof(sessionBuilder.AutoClose));
            Assert.That(sessionBuilder.AutoJoinTransaction(true), 
                Is.SameAs(sessionBuilder), nameof(sessionBuilder.AutoJoinTransaction));
            Assert.That(sessionBuilder.ConnectionReleaseMode(ConnectionReleaseMode.AfterTransaction), 
                Is.SameAs(sessionBuilder), nameof(sessionBuilder.ConnectionReleaseMode));
            Assert.That(sessionBuilder.FlushMode(FlushMode.Manual), 
                Is.SameAs(sessionBuilder), nameof(sessionBuilder.FlushMode));

            var session = sessionBuilder.OpenSession();
            Assert.That(session, Is.Not.Null, nameof(sessionBuilder.OpenSession));
        }
    }
}
