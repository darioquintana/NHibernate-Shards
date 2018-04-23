using NUnit.Framework;

namespace NHibernate.Shards.Test.Session
{
    [TestFixture]
	public class ShardedSessionImplTests: ShardedTestCase
	{
        [Test]
		public void CanCreateCriteria()
		{
			var shardedSession = this.SessionFactory.OpenSession();
			Assert.That(shardedSession.CreateCriteria("a"), Is.Not.Null);
		}

	    [Test]
	    public void CanQueryOver()
	    {
            var shardedSession = this.SessionFactory.OpenSession();
	        Assert.That(shardedSession.QueryOver<object>(), Is.Not.Null);
	    }

	    [Test]
	    public void CanCreateSharedSession()
	    {
	        var shardedSession = this.SessionFactory.OpenSession();
	        var sessionBuilder = shardedSession.SessionWithOptions();
	        Assert.That(sessionBuilder, Is.Not.Null, nameof(this.SessionFactory.WithOptions));

	        Assert.That(sessionBuilder.AutoClose(),
	            Is.SameAs(sessionBuilder), nameof(sessionBuilder.AutoClose));
	        Assert.That(sessionBuilder.AutoJoinTransaction(),
	            Is.SameAs(sessionBuilder), nameof(sessionBuilder.AutoJoinTransaction));
	        Assert.That(sessionBuilder.ConnectionReleaseMode(),
	            Is.SameAs(sessionBuilder), nameof(sessionBuilder.ConnectionReleaseMode));
	        Assert.That(sessionBuilder.FlushMode(),
	            Is.SameAs(sessionBuilder), nameof(sessionBuilder.FlushMode));

	        var sharedShardedSession = sessionBuilder.OpenSession();
	        Assert.That(sharedShardedSession, Is.Not.Null, nameof(sessionBuilder.OpenSession));
	    }
    }
}
