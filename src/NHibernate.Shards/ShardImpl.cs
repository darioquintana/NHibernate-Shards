using System.Collections.Generic;
using NHibernate.Engine;
using NHibernate.Shards.Criteria;
using NHibernate.Shards.Query;
using NHibernate.Shards.Session;
using NHibernate.Shards.Util;
using NHibernate.Shards.Engine;

namespace NHibernate.Shards
{
	public class ShardImpl : IShard
	{
        private IShardedSessionImplementor shardedSession;

        // ids of virtual shards mapped to this physical shard
		private readonly ICollection<ShardId> shardIds;

		// the SessionFactory that owns this Session
		private readonly ISessionFactoryImplementor sessionFactory;

		// the interceptor to be used when instantiating the Session
		private readonly IInterceptor interceptor;

		// the Set of shardIds is immutable, so we calculate the hashcode once up
		// front and hole on to it as an optimization
		private readonly int hashCode;

		// the actual Session!  Will be null until someone calls establishSession()
		private ISession session;

		// maps criteria ids to Criteria objects for quick lookup
		private IDictionary<CriteriaId, ICriteria> criteriaMap = new Dictionary<CriteriaId, ICriteria>();

		private IDictionary<QueryId, IQuery> queryMap = new Dictionary<QueryId, IQuery>();

		private IDictionary<CriteriaId, LinkedList<ICriteriaEvent>> criteriaEventMap =
			new Dictionary<CriteriaId, LinkedList<ICriteriaEvent>>();

		private IDictionary<QueryId, LinkedList<IQueryEvent>> queryEventMap =
			new Dictionary<QueryId, LinkedList<IQueryEvent>>();


        public ShardImpl(IShardedSessionImplementor shardedSession, IShardMetadata shardMetadata)
        {
            // make a copy to be safe
            this.shardedSession = shardedSession;
            this.shardIds = new HashSet<ShardId>(shardMetadata.ShardIds); //TODO:make it a readonly Set
            this.sessionFactory = shardMetadata.SessionFactory;
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
		public ICollection<ShardId> ShardIds
		{
			get { return shardIds; }
		}

		/// <summary>
		/// establish a Session using the SessionFactoryImplementor associated
		/// with this Shard and Apply any OpenSessionEvents that have been added.  If
		/// the Session has already been established just return it.
		/// </summary>
		/// <returns></returns>
		public ISession EstablishSession()
		{
			if (session == null)
			{
				if (interceptor == null)
				{
					session = sessionFactory.OpenSession();
				}
				else
				{
					session = sessionFactory.OpenSession(interceptor);
				}
			}
			return session;
		}

		public ICriteria GetCriteriaById(CriteriaId id)
		{
			ICriteria value;
			criteriaMap.TryGetValue(id, out value);
			return value;
		}

		public void AddCriteriaEvent(CriteriaId id, ICriteriaEvent @event)
		{
			AddEventToMap(criteriaEventMap, id, @event);
		}

		private static void AddEventToMap<TKey, TValue>(IDictionary<TKey, LinkedList<TValue>> map, TKey id, TValue eventToAdd)
		{
			Preconditions.CheckNotNull(id);
			Preconditions.CheckNotNull(eventToAdd);
			LinkedList<TValue> eventValues;
			map.TryGetValue(id, out eventValues);
			if (eventValues == null)
			{
				eventValues = new LinkedList<TValue>();
				map.Add(id, eventValues);
			}
			eventValues.AddLast(eventToAdd);
		}


		public ICriteria EstablishCriteria(IShardedCriteria shardedCriteria)
		{
			CriteriaId critId = shardedCriteria.CriteriaId;
			ICriteria crit = null;
			if(criteriaMap.Keys.Contains(critId))
			{
				crit = criteriaMap[critId];
			}
			if (crit == null)
			{
				crit = shardedCriteria.CriteriaFactory.CreateCriteria(EstablishSession());
				criteriaMap.Add(critId, crit);
				ICollection<ICriteriaEvent> critEvents = null;
				if(criteriaEventMap.Keys.Contains(critId))
				{
					critEvents = criteriaEventMap[critId];	
				}				
				if (critEvents != null)
				{
					foreach (ICriteriaEvent critEvent in critEvents)
					{
						critEvent.OnEvent(crit);
					}
					critEvents.Clear();
				}
			}
			return crit;
		}

		/// <summary>
		/// @see Criteria#list()
		/// </summary>
		/// <param name="criteriaId"></param>
		/// <returns></returns>
		public IList<object> List(CriteriaId criteriaId)
		{
			return criteriaMap[criteriaId].List<object>();
		}

		/// <summary>
		/// @see Criteria#uniqueResult()
		/// </summary>
		/// <param name="criteriaId"></param>
		/// <returns></returns>
		public object UniqueResult(CriteriaId criteriaId)
		{
			return criteriaMap[criteriaId].UniqueResult();
		}

		/// <summary>
		/// Return a <see cref="IQuery"/> by a <see cref="QueryId"/>
		/// </summary>
		/// <param name="queryId">id of the Query</param>
		/// <returns>the Query uniquely identified by the given id (unique to the Shard)</returns>
		public IQuery GetQueryById(QueryId queryId)
		{
			return queryMap[queryId];
		}

		/// <summary>
		/// TODO: Documentation
		/// </summary>
		/// <param name="id">the id of the Query with which the event should be associated</param>
		/// <param name="event">the event to add</param>
		public void AddQueryEvent(QueryId id, IQueryEvent @event)
		{
			AddEventToMap(queryEventMap, id, @event);
		}

		/// <summary>
		/// TODO: documentation
		/// </summary>
		/// <param name="shardedQuery">the ShardedQuery for which this Shard should create an actual <see cref="IQuery"/> object.</param>
		/// <returns>Query for the given ShardedQuery</returns>
		public IQuery EstablishQuery(IShardedQuery shardedQuery)
		{
			QueryId queryId = shardedQuery.QueryId;
			IQuery query;
			queryMap.TryGetValue(queryId,out query);
			if (query == null)
			{
				query = shardedQuery.QueryFactory.CreateQuery(EstablishSession());
				queryMap.Add(queryId, query);
				LinkedList<IQueryEvent> queryEvents;
				queryEventMap.TryGetValue(queryId, out queryEvents);
				if (queryEvents != null)
				{
					foreach (IQueryEvent queryEvent in queryEvents)
					{
						queryEvent.OnEvent(query);
					}
					queryEvents.Clear();
				}
			}
			return query;
		}

		/// <summary>
		/// TODO: documentation
		/// IQuery#List()
		/// </summary>
		/// <param name="queryId"></param>
		/// <returns></returns>
		public IList<object> List(QueryId queryId)
		{
			return queryMap[queryId].List<object>();
		}

		/// <summary>
		/// TODO: documentation
		/// IQuery#UniqueResult()
		/// </summary>
		/// <param name="queryId"></param>
		/// <returns></returns>
		public object UniqueResult(QueryId queryId)
		{
			return queryMap[queryId].UniqueResult();
		}
	}
}