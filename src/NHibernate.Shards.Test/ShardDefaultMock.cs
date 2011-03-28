using System;
using System.Collections.Generic;

using Iesi.Collections.Generic;

namespace NHibernate.Shards.Test
{
    public class ShardDefaultMock: IShard
    {
        public virtual ISessionFactory SessionFactory
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ISession Session
        {
            get { throw new NotSupportedException(); }
        }

        public virtual Set<ShardId> ShardIds
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ISession EstablishSession()
        {
            throw new NotSupportedException();
        }

        public virtual bool Contains(object entity)
        {
            throw new NotSupportedException();
        }

        public virtual NHibernate.Engine.ISessionFactoryImplementor SessionFactoryImplementor
        {
            get { throw new NotSupportedException(); }
        }

        public virtual void AddOpenSessionEvent(NHibernate.Shards.Session.IOpenSessionEvent @event)
        {
            throw new NotSupportedException();
        }

        public virtual ICriteria GetCriteriaById(NHibernate.Shards.Criteria.CriteriaId id)
        {
            throw new NotSupportedException();
        }

        public virtual void AddCriteriaEvent(NHibernate.Shards.Criteria.CriteriaId id, NHibernate.Shards.Criteria.ICriteriaEvent @event)
        {
            throw new NotSupportedException();
        }

        public virtual ICriteria EstablishCriteria(NHibernate.Shards.Criteria.IShardedCriteria shardedCriteria)
        {
            throw new NotSupportedException();
        }

        public virtual IList<object> List(NHibernate.Shards.Criteria.CriteriaId criteriaId)
        {
            throw new NotSupportedException();
        }

        public virtual object UniqueResult(NHibernate.Shards.Criteria.CriteriaId criteriaId)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery GetQueryById(NHibernate.Shards.Query.QueryId queryId)
        {
            throw new NotSupportedException();
        }

        public virtual void AddQueryEvent(NHibernate.Shards.Query.QueryId id, NHibernate.Shards.Query.IQueryEvent @event)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery EstablishQuery(NHibernate.Shards.Query.IShardedQuery shardedQuery)
        {
            throw new NotSupportedException();
        }

        public virtual IList<object> List(NHibernate.Shards.Query.QueryId queryId)
        {
            throw new NotSupportedException();
        }

        public virtual object UniqueResult(NHibernate.Shards.Query.QueryId queryId)
        {
            throw new NotSupportedException();
        }
    }
}
