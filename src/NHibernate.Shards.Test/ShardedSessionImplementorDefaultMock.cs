using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using NHibernate.Engine;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Stat;
using NHibernate.Type;

namespace NHibernate.Shards.Test
{
    public class ShardedSessionImplementorDefaultMock: IShardedSessionImplementor
    {
        public virtual IShard AnyShard
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IEnumerable<IShard> Shards
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IEnumerable<ISession> EstablishedSessions
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ISession EstablishFor(IShard shard)
        {
            throw new NotSupportedException();
        }

        public virtual void AfterTransactionBegin(IShardedTransaction transaction)
        {
            throw new NotSupportedException();
        }

        public virtual void AfterTransactionCompletion(IShardedTransaction transaction, bool? success)
        {
            throw new NotSupportedException();
        }

        public virtual ISession GetSessionForObject(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual ShardId GetShardIdForObject(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void LockShard()
        {
            throw new NotSupportedException();
        }

        public virtual void ApplyActionToShards(Action<ISession> action)
        {
            throw new NotSupportedException();
        }

        public virtual T Execute<T>(IShardOperation<T> operation, IExitStrategy<T> exitStrategy)
        {
            throw new NotSupportedException();
        }

        public virtual void Dispose()
        {
            throw new NotSupportedException();
        }

        public virtual void Flush()
        {
            throw new NotSupportedException();
        }

        public virtual IDbConnection Disconnect()
        {
            throw new NotSupportedException();
        }

        public virtual void Reconnect()
        {
            throw new NotSupportedException();
        }

        public virtual void Reconnect(IDbConnection connection)
        {
            throw new NotSupportedException();
        }

        public virtual IDbConnection Close()
        {
            throw new NotSupportedException();
        }

        public virtual void CancelQuery()
        {
            throw new NotSupportedException();
        }

        public virtual bool IsDirty()
        {
            throw new NotSupportedException();
        }

        public virtual object GetIdentifier(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual bool Contains(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void Evict(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual object Load(System.Type theType, object id, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual object Load(string entityName, object id, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual object Load(System.Type theType, object id)
        {
            throw new NotSupportedException();
        }

        public virtual T Load<T>(object id, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual T Load<T>(object id)
        {
            throw new NotSupportedException();
        }

        public virtual object Load(string entityName, object id)
        {
            throw new NotSupportedException();
        }

        public virtual void Load(object obj, object id)
        {
            throw new NotSupportedException();
        }

        public virtual void Replicate(object obj, ReplicationMode replicationMode)
        {
            throw new NotSupportedException();
        }

        public virtual void Replicate(string entityName, object obj, ReplicationMode replicationMode)
        {
            throw new NotSupportedException();
        }

        public virtual object Save(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void Save(object obj, object id)
        {
            throw new NotSupportedException();
        }

        public virtual object Save(string entityName, object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void SaveOrUpdate(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void SaveOrUpdate(string entityName, object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void Update(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void Update(object obj, object id)
        {
            throw new NotSupportedException();
        }

        public virtual void Update(string entityName, object obj)
        {
            throw new NotSupportedException();
        }

        public virtual object Merge(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual object Merge(string entityName, object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void Persist(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void Persist(string entityName, object obj)
        {
            throw new NotSupportedException();
        }

        public virtual object SaveOrUpdateCopy(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual object SaveOrUpdateCopy(object obj, object id)
        {
            throw new NotSupportedException();
        }

        public virtual void Delete(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void Delete(string entityName, object obj)
        {
            throw new NotSupportedException();
        }

        public virtual int Delete(string query)
        {
            throw new NotSupportedException();
        }

        public virtual int Delete(string query, object value, IType type)
        {
            throw new NotSupportedException();
        }

        public virtual int Delete(string query, object[] values, IType[] types)
        {
            throw new NotSupportedException();
        }

        public virtual void Lock(object obj, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual void Lock(string entityName, object obj, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual void Refresh(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual void Refresh(object obj, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual LockMode GetCurrentLockMode(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual ITransaction BeginTransaction()
        {
            throw new NotSupportedException();
        }

        public virtual ITransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }

        public virtual ICriteria CreateCriteria<T>() where T : class
        {
            throw new NotSupportedException();
        }

        public virtual ICriteria CreateCriteria<T>(string alias) where T : class
        {
            throw new NotSupportedException();
        }

        public virtual ICriteria CreateCriteria(System.Type persistentClass)
        {
            throw new NotSupportedException();
        }

        public virtual ICriteria CreateCriteria(System.Type persistentClass, string alias)
        {
            throw new NotSupportedException();
        }

        public virtual ICriteria CreateCriteria(string entityName)
        {
            throw new NotSupportedException();
        }

        public virtual ICriteria CreateCriteria(string entityName, string alias)
        {
            throw new NotSupportedException();
        }

        public virtual IQueryOver<T, T> QueryOver<T>() where T : class
        {
            throw new NotSupportedException();
        }

        public virtual IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
        {
            throw new NotSupportedException();
        }

        public virtual IQuery CreateQuery(string queryString)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery CreateFilter(object collection, string queryString)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery GetNamedQuery(string queryName)
        {
            throw new NotSupportedException();
        }

        public virtual ISQLQuery CreateSQLQuery(string queryString)
        {
            throw new NotSupportedException();
        }

        public virtual void Clear()
        {
            throw new NotSupportedException();
        }

        public virtual object Get(System.Type clazz, object id)
        {
            throw new NotSupportedException();
        }

        public virtual object Get(System.Type clazz, object id, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual object Get(string entityName, object id)
        {
            throw new NotSupportedException();
        }

        public virtual T Get<T>(object id)
        {
            throw new NotSupportedException();
        }

        public virtual T Get<T>(object id, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual string GetEntityName(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual IFilter EnableFilter(string filterName)
        {
            throw new NotSupportedException();
        }

        public virtual IFilter GetEnabledFilter(string filterName)
        {
            throw new NotSupportedException();
        }

        public virtual void DisableFilter(string filterName)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery CreateMultiQuery()
        {
            throw new NotSupportedException();
        }

        public virtual ISession SetBatchSize(int batchSize)
        {
            throw new NotSupportedException();
        }

        public virtual ISessionImplementor GetSessionImplementation()
        {
            throw new NotSupportedException();
        }

        public virtual IMultiCriteria CreateMultiCriteria()
        {
            throw new NotSupportedException();
        }

        public virtual ISession GetSession(EntityMode entityMode)
        {
            throw new NotSupportedException();
        }

        public virtual EntityMode ActiveEntityMode
        {
            get { throw new NotSupportedException(); }
        }

        public virtual FlushMode FlushMode
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public virtual CacheMode CacheMode
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public virtual ISessionFactory SessionFactory
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IDbConnection Connection
        {
            get { throw new NotSupportedException(); }
        }

        public virtual bool IsOpen
        {
            get { throw new NotSupportedException(); }
        }

        public virtual bool IsConnected
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ITransaction Transaction
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ISessionStatistics Statistics
        {
            get { throw new NotSupportedException(); }
        }
    }
}
