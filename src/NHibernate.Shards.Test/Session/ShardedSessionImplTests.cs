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
    }
}
