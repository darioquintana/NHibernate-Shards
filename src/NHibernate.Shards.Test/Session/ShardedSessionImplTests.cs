using NHibernate.Cfg;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Session
{
    using System;
    using System.Collections.Generic;
    using NHibernate.Mapping.ByCode;
    using NHibernate.Shards.Mapping.ByCode;

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
