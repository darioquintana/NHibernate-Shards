using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NHibernate.Shards.Test.Mock;
using NHibernate.Engine;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Session;
using NHibernate.Shards.Strategy;

namespace NHibernate.Shards.Test.Session
{

	[TestFixture]
	public class ShardedSessionImplTests: ShardedTestCase
	{
		[Test]
		public void CanCreateCriteria()
		{
			var shardedSession = this.SessionFactory.OpenSession();
			var criteria = shardedSession.CreateCriteria("a");
			Assert.That(criteria, Is.Not.Null);
		}
	}
}
