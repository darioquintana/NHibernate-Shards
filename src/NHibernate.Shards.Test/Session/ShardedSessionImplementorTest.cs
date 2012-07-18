using System.Collections.Generic;
using NHibernate.Engine;
using NHibernate.Shards.Session;
using NHibernate.Shards.Strategy.Access;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Session
{
    using Engine;

    [TestFixture]
    public class ShardedSessionImplementorTest
    {
        #region SetUp

        [SetUp]
        public void SetUp()
        {
            NHibernate.LoggerProvider.SetLoggersFactory(new NoLoggingLoggerFactory());
        }

        #endregion

        #region Tests

        [Test]
        public void CanCreateQuery()
        {
            var hql = "from SomeEntity";
            var session = new MyShardedSessionImpl();
            var query = session.CreateQuery(hql);
            Assert.That(query, Is.Not.Null);
            Assert.That(query.QueryString, Is.EqualTo(hql));
        }

        [Test]
        public void CanCreateSQLQuery()
        {
            var sql = "UPDATE some_entity SET some_field = 'some_value'";
            var session = new MyShardedSessionImpl();
            var sqlQuery = session.CreateSQLQuery(sql);
            Assert.That(sqlQuery, Is.Not.Null);
            Assert.That(sqlQuery.QueryString, Is.EqualTo(sql));
        }

        [Test]
        public void CanCreateMultiQuery()
        {
            var session = new MyShardedSessionImpl();
            var multiQuery = session.CreateMultiQuery();
            Assert.That(multiQuery, Is.Not.Null);
        }

        #endregion

        #region Mock classes

        private class MyShardStrategy : ShardStrategyDefaultMock
        {
            public override IShardAccessStrategy ShardAccessStrategy
            {
                get { return new SequentialShardAccessStrategy(); }
            }
        }

        private class MyShardedSessionFactory : ShardedSessionFactoryDefaultMock
        {
            public override IList<ISessionFactory> SessionFactories
            {
                get { return new ISessionFactory[0]; }
            }

            public override IEnumerable<IShardMetadata> GetShardMetadata()
            {
                return new IShardMetadata[]
                {
                    new ShardMetadataImpl(new ShardId(0), new MySessionFactoryImplementor()),
                };
            }
        }

        private class MyShardedSessionImpl: ShardedSessionImpl
        {
            public MyShardedSessionImpl()
                : base(new MyShardedSessionFactory(), new MyShardStrategy(), System.Type.EmptyTypes, null, true)
            {}
        }

        private class MySessionFactoryImplementor : SessionFactoryImplementorDefaultMock
        {
            public override ISession OpenSession()
            {
                return new MySession();
            }

            public override ISession OpenSession(IInterceptor sessionLocalInterceptor)
            {
                return new MySession();
            }
        }

        private class MySession : SessionDefaultMock
        {
            public override IMultiQuery CreateMultiQuery()
            {
                return new MultiQueryDefaultMock();
            }

            public override IQuery CreateQuery(string hql)
            {
                return new MyQuery(hql);
            }

            public override ISQLQuery CreateSQLQuery(string queryString)
            {
                return new MySQLQuery(queryString);
            }
        }

        private class MyQuery : QueryDefaultMock
        {
            private string queryString;

            public MyQuery(string queryString)
            {
                this.queryString = queryString;
            }

            public override string QueryString
            {
                get { return queryString; }
            }
        }

        private class MySQLQuery : SQLQueryDefaultMock
        {
            private string queryString;

            public MySQLQuery(string queryString)
            {
                this.queryString = queryString;
            }

            public override string QueryString
            {
                get { return queryString; }
            }
        }


        #endregion
    }
}
