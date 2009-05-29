using System;
using System.Collections;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using LinFu.DynamicProxy;
using NHibernate.Engine;
using NHibernate.Shards.Criteria;
using NHibernate.Shards.Query;
using NHibernate.Shards.Session;
using NHibernate.Shards.Util;

namespace NHibernate.Shards
{
    public class ShardImpl : IShard
    {
        // ids of virtual shards mapped to this physical shard
        private readonly Set<ShardId> shardIds;

        // the SessionFactory that owns this Session
        private readonly ISessionFactoryImplementor sessionFactory;

        // the interceptor to be used when instantiating the Session
        private readonly Interceptor interceptor;

        // the Set of shardIds is immutable, so we calculate the hashcode once up
        // front and hole on to it as an optimization
        private readonly int hashCode;

        // the actual Session!  Will be null until someone calls establishSession()
        private ISession session;

        // events that need to fire when the Session is opened
        private readonly LinkedList<IOpenSessionEvent> openSessionEvents = new LinkedList<IOpenSessionEvent>();

        // maps criteria ids to Criteria objects for quick lookup
        private Dictionary<CriteriaId, ICriteria> criteriaMap = new Dictionary<CriteriaId, ICriteria>();

        public ShardImpl(ShardId shardId, ISessionFactoryImplementor sessionFactory)//:this(Collections sessionFactory, null)
        {
            //this(Collections.singleton(shardId), sessionFactory, null);
        }

        public ShardImpl(Set<ShardId> shardIds, ISessionFactoryImplementor sessionFactory) : this(shardIds, sessionFactory, null)
        {
            //this(shardIds, sessionFactory, null);
        }

        public ShardImpl(Set<ShardId> shardIds,ISessionFactoryImplementor sessionFactory,/*@Nullable*/ Interceptor interceptor)
        {
            // make a copy to be safe
            //this.shardIds = Collections.unmodifiableSet(Sets.newHashSet(shardIds));
            hashCode = shardIds.GetHashCode();
            this.sessionFactory = sessionFactory;
            this.interceptor = interceptor;
        }

        /// <summary>
        /// SessionFactoryImplementor that owns the Session associated with this Shard
        /// </summary>
        public ISessionFactoryImplementor SessionFactoryImplementor
        {
            get { return sessionFactory; }
        }

        /// <summary>
        /// the Session associated with this Shard.  Will return null if
        /// the Session has not yet been established.
        /// </summary>
        public ISession Session
        {
            get { return session; }
        }

        /// <summary>
        /// Ids of the virtual shards that are mapped to this physical shard.
        /// The returned Set is unmodifiable.
        /// </summary>
        public Set<ShardId> ShardIds
        {
            get { throw new System.NotImplementedException(); }
        }

        /// <summary>
        /// Add a open Session event 
        /// </summary>
        /// <param name="event">the event to add</param>
        public void AddOpenSessionEvent(IOpenSessionEvent @event)
        {
            Preconditions.CheckNotNull(@event);
            openSessionEvents.AddLast(@event);
        }

        /// <summary>
        /// establish a Session using the SessionFactoryImplementor associated
        /// with this Shard and Apply any OpenSessionEvents that have been added.  If
        /// the Session has already been established just return it.
        /// </summary>
        /// <returns></returns>
        public ISession EstablishSession()
        {
            throw new System.NotImplementedException();
        }

        public ICriteria GetCriteriaById(CriteriaId id)
        {
            throw new NotImplementedException();
        }

        public void AddCriteriaEvent(CriteriaId id, ICriteriaEvent @event)
        {
            throw new NotImplementedException();
        }

        public ICriteria EstablishCriteria(IShardedCriteria shardedCriteria)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// @see Criteria#list()
        /// </summary>
        /// <param name="criteriaId"></param>
        /// <returns></returns>
        public IList<object> List(CriteriaId criteriaId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// @see Criteria#uniqueResult()
        /// </summary>
        /// <param name="criteriaId"></param>
        /// <returns></returns>
        public object UniqueResult(CriteriaId criteriaId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Return a <see cref="IQuery"/> by a <see cref="QueryId"/>
        /// </summary>
        /// <param name="queryId">id of the Query</param>
        /// <returns>the Query uniquely identified by the given id (unique to the Shard)</returns>
        public IQuery GetQueryById(QueryId queryId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// TODO: Documentation
        /// </summary>
        /// <param name="id">the id of the Query with which the event should be associated</param>
        /// <param name="event">the event to add</param>
        public void AddQueryEvent(QueryId id, IQueryEvent @event)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// TODO: documentation
        /// </summary>
        /// <param name="shardedQuery">the ShardedQuery for which this Shard should create an actual <see cref="IQuery"/> object.</param>
        /// <returns>Query for the given ShardedQuery</returns>
        public IQuery EstablishQuery(IShardedQuery shardedQuery)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// TODO: documentation
        /// IQuery#List()
        /// </summary>
        /// <param name="queryId"></param>
        /// <returns></returns>
        public IList<object> List(QueryId queryId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// TODO: documentation
        /// IQuery#UniqueResult()
        /// </summary>
        /// <param name="queryId"></param>
        /// <returns></returns>
        public object UniqueResult(QueryId queryId)
        {
            throw new System.NotImplementedException();
        }

        ///<summary>
        /// <returns>the OpenSessionEvents that are waiting to fire</returns>
        /// </summary>
        public LinkedList<IOpenSessionEvent> GetOpenSessionEvents()
        {
            //return openSessionEvents;
            return null;
        }
    }
}