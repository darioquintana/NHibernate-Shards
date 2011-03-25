using System;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Shards.Criteria;
using NHibernate.Shards.Query;
using NHibernate.Shards.Session;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;

namespace NHibernate.Shards.Test
{
    [TestFixture,Ignore]
    public class ShardImplTest : TestFixtureBaseWithMock
    {
          [Test]
          public void TestAddOpenSessionEvent() {
              ShardImpl shard = new ShardImpl(new ShardId(1), Stub<ISessionFactoryImplementor>());//new SessionFactoryDefaultMock()
              try
              {
                  shard.AddOpenSessionEvent(null);
                  Assert.Fail("expected nre");
              }
              catch (NullReferenceException nre)
              {
                  // good
              }
              IOpenSessionEvent ose = Stub<IOpenSessionEvent>();//new OpenSessionEventDefaultMock()
              shard.AddOpenSessionEvent(ose);
              Assert.IsNotNull(shard.GetOpenSessionEvents());
              Assert.AreEqual(1, shard.GetOpenSessionEvents().Count);//.size()
              //Assert.AreSame(ose, shard.GetOpenSessionEvents().Find(0));//.get(0)

              // now add another and make sure it is added to the end
              IOpenSessionEvent anotherOse = Stub<IOpenSessionEvent>();
              shard.AddOpenSessionEvent(anotherOse);
              Assert.IsNotNull(shard.GetOpenSessionEvents());
              Assert.AreEqual(2, shard.GetOpenSessionEvents().Count);//.size()
              //Assert.AreSame(ose, shard.GetOpenSessionEvents().get(0));
              //Assert.AreSame(anotherOse, shard.GetOpenSessionEvents().get(1));
          }

        [Test]
        public void TestAddCriteriaEvent()
        {
            ShardImpl shard = new ShardImpl(new ShardId(1), Stub<ISessionFactoryImplementor>());//new SessionFactoryDefaultMock()
            try
            {
                shard.AddCriteriaEvent(null, null);
                Assert.Fail("expected nre");
            }
            catch (NullReferenceException nre)
            {
                // good
            }

            CriteriaId criteriaId = new CriteriaId(2);
            try
            {
                shard.AddCriteriaEvent(criteriaId, null);
                Assert.Fail("expected nre");
            }
            catch (NullReferenceException nre)
            {
                // good
            }

            ICriteriaEvent ce = Stub<ICriteriaEvent>();//new CriteriaEventDefaultMock()
            try
            {
                shard.AddCriteriaEvent(null, ce);
                Assert.Fail("expected nre");
            }
            catch (NullReferenceException nre)
            {
                // good
            }

            shard.AddCriteriaEvent(criteriaId, ce);
            //Assert.IsNotNull(shard.GetCriteriaEventMap());
            //Assert.Equals(1, shard.getCriteriaEventMap().size());
            //Assert.Equals(1, shard.getCriteriaEventMap().get(criteriaId).size());
            //Assert.AreSame(ce, shard.getCriteriaEventMap().get(criteriaId).get(0));

            // now add another event to the same criteria
            ICriteriaEvent anotherCe = Stub<ICriteriaEvent>();
            //shard.AddCriteriaEvent(criteriaId, anotherCe);
            //Assert.IsNotNull(shard.getCriteriaEventMap());
            //Assert.Equals(1, shard.getCriteriaEventMap().size());
            //Assert.Equals(2, shard.getCriteriaEventMap().get(criteriaId).size());
            //Assert.AreSame(ce, shard.getCriteriaEventMap().get(criteriaId).get(0));
            //Assert.AreSame(anotherCe, shard.getCriteriaEventMap().get(criteriaId).get(1));

            // now add an event to a different criteria
            CriteriaId anotherCriteriaId = new CriteriaId(3);
            ICriteriaEvent yetAnotherCe = Stub<ICriteriaEvent>();
            shard.AddCriteriaEvent(anotherCriteriaId, yetAnotherCe);
            //Assert.IsNotNull(shard.getCriteriaEventMap());
            //Assert.Equals(2, shard.getCriteriaEventMap().size());
            //Assert.Equals(2, shard.getCriteriaEventMap().get(criteriaId).size());
            //Assert.AreSame(ce, shard.getCriteriaEventMap().get(criteriaId).get(0));
            //Assert.AreSame(anotherCe, shard.getCriteriaEventMap().get(criteriaId).get(1));
            //Assert.Equals(1, shard.getCriteriaEventMap().get(anotherCriteriaId).size());
            //Assert.AreSame(yetAnotherCe, shard.getCriteriaEventMap().get(anotherCriteriaId).get(0));
        }

        //[Test]
        //public void TestEstablishSessionNoEvents()
        //{
        //    MySessionFactory sf = new MySessionFactory();
        //    ShardImpl shardImpl = new ShardImpl(new ShardId(1), sf);
        //    shardImpl.EstablishSession();
        //    Assert.Equals(1, sf.numOpenSessionCalls);
        //    shardImpl.EstablishSession();
        //    Assert.Equals(1, sf.numOpenSessionCalls);
        //}

        //[Test]
        //public void TestEstablishSessionNoEventsWithInterceptor()
        //{
        //    MySessionFactory sf = new MySessionFactory();
        //    Interceptor interceptor = new InterceptorDefaultMock();
        //    ShardImpl shardImpl = new ShardImpl(Sets.newHashSet(new ShardId(1)), sf, interceptor);
        //    shardImpl.establishSession();
        //    Assert.Equals(1, sf.numOpenSessionWithInterceptorCalls);
        //    shardImpl.establishSession();
        //    Assert.Equals(1, sf.numOpenSessionWithInterceptorCalls);
        //}

        //[Test]
        //public void TestEstablishSessionWithEvents()
        //{
        //    MySessionFactory sf = new MySessionFactory();
        //    ShardImpl shardImpl = new ShardImpl(new ShardId(1), sf);
        //    MyOpenSessionEvent event1 = new MyOpenSessionEvent();
        //    MyOpenSessionEvent event2 = new MyOpenSessionEvent();
        //    shardImpl.AddOpenSessionEvent(event1);
        //    shardImpl.AddOpenSessionEvent(event2);
        //    shardImpl.EstablishSession();
        //    Assert.Equals(1, sf.numOpenSessionCalls);
        //    Assert.Equals(1, event1.numOnOpenSessionCalls);
        //    Assert.Equals(1, event2.numOnOpenSessionCalls);
        //    Assert.IsTrue(shardImpl.getOpenSessionEvents().isEmpty());
        //    shardImpl.establishSession();
        //    AssertEquals(1, sf.numOpenSessionCalls);
        //    AssertEquals(1, event1.numOnOpenSessionCalls);
        //    AssertEquals(1, event2.numOnOpenSessionCalls);
        //    AssertTrue(shardImpl.getOpenSessionEvents().isEmpty());
        //}

        //[Test]
        //public void TestEstablishCriteriaNoEvents()
        //{
        //    MySessionFactory sf = new MySessionFactory();
        //    ShardImpl shardImpl = new ShardImpl(new ShardId(1), sf);
        //    CriteriaId critId = new CriteriaId(3);
        //    ICriteria crit = new CriteriaDefaultMock();
        //    MyCriteriaFactory mcf = new MyCriteriaFactory(crit);
        //    MyShardedCriteria msc = new MyShardedCriteria(critId, mcf);
        //    shardImpl.EstablishCriteria(msc);
        //    Assert.Equals(1, sf.numOpenSessionCalls);
        //    Assert.IsNotNull(mcf.createCriteriaCalledWith);
        //    Assert.Equals(1, shardImpl.getCriteriaMap().size());
        //    Assert.AreSame(crit, shardImpl.getCriteriaMap().get(critId));

        //    shardImpl.establishCriteria(msc);
        //    Assert.Equals(1, sf.numOpenSessionCalls);
        //    Assert.IsNotNull(mcf.createCriteriaCalledWith);
        //    Assert.Equals(1, shardImpl.getCriteriaMap().size());
        //    Assert.AreSame(crit, shardImpl.getCriteriaMap().get(critId));
        //}

        //[Test]
        //public void TestEstablishCriteriaWithEvents()
        //{
        //    MySessionFactory sf = new MySessionFactory();
        //    ShardImpl shardImpl = new ShardImpl(new ShardId(1), sf);
        //    MyCriteriaEvent event1 = new MyCriteriaEvent();
        //    MyCriteriaEvent event2 = new MyCriteriaEvent();
        //    CriteriaId critId = new CriteriaId(3);
        //    shardImpl.AddCriteriaEvent(critId, event1);
        //    shardImpl.AddCriteriaEvent(critId, event2);
        //    ICriteria crit = Stub<ICriteria>() ;//new CriteriaDefaultMock()
        //    MyCriteriaFactory mcf = new MyCriteriaFactory(crit);
        //    MyShardedCriteria msc = new MyShardedCriteria(critId, mcf);
        //    shardImpl.establishCriteria(msc);
        //    Assert.Equals(1, sf.numOpenSessionCalls);
        //    Assert.IsNotNull(mcf.createCriteriaCalledWith);
        //    Assert.Equals(1, shardImpl.getCriteriaMap().size());
        //    Assert.AreSame(crit, shardImpl.getCriteriaMap().get(critId));
        //    Assert.Equals(1, event1.numOnEventCalls);
        //    Assert.Equals(1, event2.numOnEventCalls);
        //    Assert.IsTrue(shardImpl.getCriteriaEventMap().get(critId).isEmpty());

        //    shardImpl.establishCriteria(msc);
        //    Assert.Equals(1, sf.numOpenSessionCalls);
        //    Assert.IsNotNull(mcf.createCriteriaCalledWith);
        //    Assert.Equals(1, shardImpl.getCriteriaMap().size());
        //    Assert.AreSame(crit, shardImpl.getCriteriaMap().get(critId));
        //    Assert.Equals(1, event1.numOnEventCalls);
        //    Assert.Equals(1, event2.numOnEventCalls);
        //    Assert.IsTrue(shardImpl.getCriteriaEventMap().get(critId).isEmpty());
        //}

        //[Test]
        //public void TestEstablishMultipleCriteria()
        //{
        //    MySessionFactory sf = new MySessionFactory();
        //    ShardImpl shardImpl = new ShardImpl(new ShardId(1), sf);

        //    MyCriteriaEvent event1 = new MyCriteriaEvent();
        //    MyCriteriaEvent event2 = new MyCriteriaEvent();
        //    CriteriaId critId1 = new CriteriaId(3);
        //    ICriteria crit1 = new CriteriaDefaultMock();
        //    MyCriteriaFactory mcf1 = new MyCriteriaFactory(crit1);
        //    MyShardedCriteria msc1 = new MyShardedCriteria(critId1, mcf1);

        //    shardImpl.AddCriteriaEvent(critId1, event1);
        //    shardImpl.AddCriteriaEvent(critId1, event2);

        //    CriteriaId critId2 = new CriteriaId(4);
        //    ICriteria crit2 = new CriteriaDefaultMock();
        //    MyCriteriaFactory mcf2 = new MyCriteriaFactory(crit2);
        //    MyShardedCriteria msc2 = new MyShardedCriteria(critId2, mcf2);

        //    shardImpl.establishCriteria(msc1);
        //    shardImpl.establishCriteria(msc2);

        //    Assert.Equals(1, sf.numOpenSessionCalls);
        //    Assert.Equals(2, shardImpl.getCriteriaMap().size());

        //    AssertNotNull(mcf1.createCriteriaCalledWith);
        //    AssertSame(crit1, shardImpl.getCriteriaMap().get(critId1));
        //    Assert.Equals(1, event1.numOnEventCalls);
        //    Assert.Equals(1, event2.numOnEventCalls);
        //    AssertTrue(shardImpl.getCriteriaEventMap().get(critId1).isEmpty());

        //    AssertNotNull(mcf2.createCriteriaCalledWith);
        //    AssertSame(crit2, shardImpl.getCriteriaMap().get(critId2));
        //    AssertNull(shardImpl.getCriteriaEventMap().get(critId2));

        //    shardImpl.establishCriteria(msc1);
        //    Assert.Equals(1, sf.numOpenSessionCalls);
        //    AssertNotNull(mcf1.createCriteriaCalledWith);
        //    Assert.Equals(2, shardImpl.getCriteriaMap().size());
        //    AssertSame(crit1, shardImpl.getCriteriaMap().get(critId1));
        //    Assert.Equals(1, event1.numOnEventCalls);
        //    Assert.Equals(1, event2.numOnEventCalls);

        //    AssertNotNull(mcf2.createCriteriaCalledWith);
        //    AssertSame(crit2, shardImpl.getCriteriaMap().get(critId2));
        //    AssertNull(shardImpl.getCriteriaEventMap().get(critId2));

        //    AssertTrue(shardImpl.getCriteriaEventMap().get(critId1).isEmpty());
        //}

        [Test]
public void TestAddQueryEvent() {
    ShardImpl shard = new ShardImpl(new ShardId(1), Stub<ISessionFactoryImplementor>());
    try {
      shard.AddQueryEvent(null, null);
      Assert.Fail("expected npe");
    } catch (NullReferenceException npe) {
      // good
    }

    QueryId queryId = new QueryId(1);
    try {
      shard.AddQueryEvent(queryId, null);
      Assert.Fail("expected npe");
    } catch (NullReferenceException npe) {
      // good
    }

    IQueryEvent qe = Stub<IQueryEvent>();
    try {
      shard.AddQueryEvent(null, qe);
      Assert.Fail("expected npe");
    } catch (NullReferenceException npe) {
      // good
    }

    shard.AddQueryEvent(queryId, qe);
    //assertNotNull(shard.getQueryEventMap());
    //assertEquals(1, shard.getQueryEventMap().size());
    //assertEquals(1, shard.getQueryEventMap().get(queryId).size());
    //assertSame(qe, shard.getQueryEventMap().get(queryId).get(0));

    // now add another event to the same query
    IQueryEvent anotherQe = Stub<IQueryEvent>();
    shard.AddQueryEvent(queryId, anotherQe);
    //assertNotNull(shard.getQueryEventMap());
    //assertEquals(1, shard.getQueryEventMap().size());
    //assertEquals(2, shard.getQueryEventMap().get(queryId).size());
    //assertSame(qe, shard.getQueryEventMap().get(queryId).get(0));
    //assertSame(anotherQe, shard.getQueryEventMap().get(queryId).get(1));

    // now add an event to a different query
    QueryId anotherQueryId = new QueryId(3);
    IQueryEvent yetAnotherQe = Stub<IQueryEvent>();
    shard.AddQueryEvent(anotherQueryId, yetAnotherQe);
    //assertNotNull(shard.getQueryEventMap());
    //assertEquals(2, shard.getQueryEventMap().size());
    //assertEquals(2, shard.getQueryEventMap().get(queryId).size());
    //assertSame(qe, shard.getQueryEventMap().get(queryId).get(0));
    //assertSame(anotherQe, shard.getQueryEventMap().get(queryId).get(1));
    //assertEquals(1, shard.getQueryEventMap().get(anotherQueryId).size());
    //assertSame(yetAnotherQe, shard.getQueryEventMap().get(anotherQueryId).get(0));
  }




  ////@Override
  //  public CriteriaId getCriteriaId() {
  //    return critId;
  //  }

  //  //@Override
  //  public ICriteriaFactory getCriteriaFactory() {
  //    return critFactory;
  //  }
  //}

  //  //@Override
  //  public ICriteria createCriteria(org.hibernate.Session session) {
  //    createCriteriaCalledWith = session;
  //    return critToReturn;
  //  }
  //}

  //private static readonly class MyCriteriaEvent implements CriteriaEvent {
  //  private int numOnEventCalls;
  //  public void onEvent(Criteria crit) {
  //    numOnEventCalls++;
  //  }
  //}

  // private static readonly class MyShardedQuery extends ShardedQueryDefaultMock {
  //  private readonly QueryId queryId;
  //  private readonly QueryFactory queryFactory;

  //  public MyShardedQuery(QueryId queryId, IQueryFactory queryFactory) {
  //    this.queryId = queryId;
  //    this.queryFactory = queryFactory;
  //  }

  //  //@Override
  //  public QueryId getQueryId() {
  //    return queryId;
  //  }

  //  //@Override
  //  public QueryFactory getQueryFactory() {
  //    return queryFactory;
  //  }
  //}

  //public static readonly class MyQueryFactory extends QueryFactoryDefaultMock {
  //  private org.hibernate.Session createQueryCalledWith;
  //  private Query queryToReturn;

  //  public MyQueryFactory(Query queryToReturn) {
  //    this.queryToReturn = queryToReturn;
  //  }

  //  public IQuery createQuery(Session session) {
  //    createQueryCalledWith = session;
  //    return queryToReturn;
  //  }
  //}

  //private static readonly class MyQueryEvent implements QueryEvent {
  //  private int numOnEventCalls;
  //  public void onEvent(Query query) {
  //    numOnEventCalls++;
    }
}
