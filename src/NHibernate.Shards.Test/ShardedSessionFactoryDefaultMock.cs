using System;
using System.Collections.Generic;
using System.Data;
using Iesi.Collections.Generic;
using NHibernate.Cache;
using NHibernate.Cfg;
using NHibernate.Connection;
using NHibernate.Context;
using NHibernate.Dialect.Function;
using NHibernate.Engine;
using NHibernate.Engine.Query;
using NHibernate.Exceptions;
using NHibernate.Id;
using NHibernate.Metadata;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Strategy;
using NHibernate.Stat;
using NHibernate.Transaction;
using NHibernate.Type;

namespace NHibernate.Shards.Test
{
    internal class ShardedSessionFactoryDefaultMock: IShardedSessionFactoryImplementor
    {
        public virtual void Dispose()
        {
            throw new NotSupportedException();
        }

        public virtual ISession OpenSession(IDbConnection conn)
        {
            throw new NotSupportedException();
        }

        public virtual IShardedSession OpenSession(IInterceptor interceptor)
        {
            throw new NotSupportedException();
        }

        public virtual IShardedSession OpenSession()
        {
            throw new NotSupportedException();
        }

        public virtual IList<ISessionFactory> SessionFactories
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IShardedSessionFactory GetSessionFactory(IEnumerable<ShardId> shardIds, IShardStrategyFactory shardStrategyFactory)
        {
            throw new NotSupportedException();
        }

        ISession ISessionFactory.OpenSession(IInterceptor sessionLocalInterceptor)
        {
            return this.OpenSession(sessionLocalInterceptor);
        }

        public virtual ISession OpenSession(IDbConnection conn, IInterceptor sessionLocalInterceptor)
        {
            throw new NotSupportedException();
        }

        ISession ISessionFactory.OpenSession()
        {
            return this.OpenSession();
        }

        public virtual IClassMetadata GetClassMetadata(System.Type persistentClass)
        {
            throw new NotSupportedException();
        }

        public virtual IClassMetadata GetClassMetadata(string entityName)
        {
            throw new NotSupportedException();
        }

        public virtual ICollectionMetadata GetCollectionMetadata(string roleName)
        {
            throw new NotSupportedException();
        }

        public virtual IDictionary<string, IClassMetadata> GetAllClassMetadata()
        {
            throw new NotSupportedException();
        }

        public virtual IDictionary<string, ICollectionMetadata> GetAllCollectionMetadata()
        {
            throw new NotSupportedException();
        }

        public virtual void Close()
        {
            throw new NotSupportedException();
        }

        public virtual void Evict(System.Type persistentClass)
        {
            throw new NotSupportedException();
        }

        public virtual void Evict(System.Type persistentClass, object id)
        {
            throw new NotSupportedException();
        }

        public virtual void EvictEntity(string entityName)
        {
            throw new NotSupportedException();
        }

        public virtual void EvictEntity(string entityName, object id)
        {
            throw new NotSupportedException();
        }

        public virtual void EvictCollection(string roleName)
        {
            throw new NotSupportedException();
        }

        public virtual void EvictCollection(string roleName, object id)
        {
            throw new NotSupportedException();
        }

        public virtual void EvictQueries()
        {
            throw new NotSupportedException();
        }

        public virtual void EvictQueries(string cacheRegion)
        {
            throw new NotSupportedException();
        }

        public virtual IStatelessSession OpenStatelessSession()
        {
            throw new NotSupportedException();
        }

        public virtual IStatelessSession OpenStatelessSession(IDbConnection connection)
        {
            throw new NotSupportedException();
        }

        public virtual FilterDefinition GetFilterDefinition(string filterName)
        {
            throw new NotSupportedException();
        }

        public virtual ISession GetCurrentSession()
        {
            throw new NotSupportedException();
        }

        public virtual IStatistics Statistics
        {
            get { throw new NotSupportedException(); }
        }

        public virtual bool IsClosed
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ICollection<string> DefinedFilterNames
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IType GetIdentifierType(string className)
        {
            throw new NotSupportedException();
        }

        public virtual string GetIdentifierPropertyName(string className)
        {
            throw new NotSupportedException();
        }

        public virtual IType GetReferencedPropertyType(string className, string propertyName)
        {
            throw new NotSupportedException();
        }

        public virtual bool HasNonIdentifierPropertyNamedId(string className)
        {
            throw new NotSupportedException();
        }

        public virtual IDictionary<string, ICache> GetAllSecondLevelCacheRegions()
        {
            throw new NotSupportedException();
        }

        public virtual IEntityPersister GetEntityPersister(string entityName)
        {
            throw new NotSupportedException();
        }

        public virtual ICollectionPersister GetCollectionPersister(string role)
        {
            throw new NotSupportedException();
        }

        public virtual IType[] GetReturnTypes(string queryString)
        {
            throw new NotSupportedException();
        }

        public virtual string[] GetReturnAliases(string queryString)
        {
            throw new NotSupportedException();
        }

        public virtual string[] GetImplementors(string className)
        {
            throw new NotSupportedException();
        }

        public virtual string GetImportedClassName(string name)
        {
            throw new NotSupportedException();
        }

        public virtual IQueryCache GetQueryCache(string regionName)
        {
            throw new NotSupportedException();
        }

        public virtual NamedQueryDefinition GetNamedQuery(string queryName)
        {
            throw new NotSupportedException();
        }

        public virtual NamedSQLQueryDefinition GetNamedSQLQuery(string queryName)
        {
            throw new NotSupportedException();
        }

        public virtual ResultSetMappingDefinition GetResultSetMapping(string resultSetRef)
        {
            throw new NotSupportedException();
        }

        public virtual IIdentifierGenerator GetIdentifierGenerator(string rootEntityName)
        {
            throw new NotSupportedException();
        }

        public virtual ICache GetSecondLevelCacheRegion(string regionName)
        {
            throw new NotSupportedException();
        }

        public virtual ISession OpenSession(IDbConnection connection, bool flushBeforeCompletionEnabled, bool autoCloseSessionEnabled, ConnectionReleaseMode connectionReleaseMode)
        {
            throw new NotSupportedException();
        }

        public virtual ISet<string> GetCollectionRolesByEntityParticipant(string entityName)
        {
            throw new NotSupportedException();
        }

        public virtual IEntityPersister TryGetEntityPersister(string entityName)
        {
            throw new NotSupportedException();
        }

        public virtual string TryGetGuessEntityName(System.Type implementor)
        {
            throw new NotSupportedException();
        }

        public virtual NHibernate.Dialect.Dialect Dialect
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IInterceptor Interceptor
        {
            get { throw new NotSupportedException(); }
        }

        public virtual QueryPlanCache QueryPlanCache
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IConnectionProvider ConnectionProvider
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ITransactionFactory TransactionFactory
        {
            get { throw new NotSupportedException(); }
        }

        public virtual UpdateTimestampsCache UpdateTimestampsCache
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IStatisticsImplementor StatisticsImplementor
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ISQLExceptionConverter SQLExceptionConverter
        {
            get { throw new NotSupportedException(); }
        }

        public virtual Settings Settings
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IEntityNotFoundDelegate EntityNotFoundDelegate
        {
            get { throw new NotSupportedException(); }
        }

        public virtual SQLFunctionRegistry SQLFunctionRegistry
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IQueryCache QueryCache
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ICurrentSessionContext CurrentSessionContext
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IEnumerable<IShardMetadata> GetShardMetadata()
        {
            throw new NotSupportedException();
        }

        public virtual bool ContainsFactory(ISessionFactoryImplementor factory)
        {
            throw new NotSupportedException();
        }

        public virtual ISessionFactoryImplementor ControlFactory
        {
            get { throw new NotSupportedException(); }
        }
    }
}
