using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Util;
using NHibernate.Transform;
using NHibernate.Type;

namespace NHibernate.Shards.Query
{
    /// <summary>
    /// Concrete implementation of ShardedQuery provided by Hibernate Shards. This
    /// implementation introduces limits to the HQL language; mostly around
    /// limits and aggregation. Its approach is simply to execute the query on
    /// each shard and compile the results in a list, or if a unique result is
    /// desired, the fist non-null result is returned.
    /// 
    /// The setFoo methods are implemented using a set of classes that implement
    /// the QueryEvent interface and are called SetFooEvent. These query events
    /// are used to call setFoo with the appropriate arguments on each Query that
    /// is executed on a shard.
    /// </summary>
    public class ShardedQueryImpl : IShardedQuery
    {
        private readonly IShardedSessionImplementor session;
        private readonly Func<ISession, IQuery> queryFactory;

        private readonly IDictionary<IShard, IQuery> establishedQueriesByShard = new Dictionary<IShard, IQuery>();
        private readonly ICollection<Action<IQuery>> establishActions = new List<Action<IQuery>>();

        private readonly ListExitOperationBuilder listExitOperationBuilder = new ListExitOperationBuilder();

        /// <summary>
        /// Creates new <see cref="ShardedQueryImpl"/> instance.
        /// </summary>
        /// <param name="session">The Sharded session on which this query is to be executed.</param>
        /// <param name="hql">An HQL query string.</param>
        public static ShardedQueryImpl CreateQuery(IShardedSessionImplementor session, string hql)
        {
            return new ShardedQueryImpl(session, s => s.CreateQuery(hql));
        }

        public static ShardedQueryImpl GetNamedQuery(IShardedSessionImplementor session, string queryName)
        {
            return new ShardedQueryImpl(session, s => s.GetNamedQuery(queryName));
        }

        /// <summary>
        /// Creates new <see cref="ShardedQueryImpl"/> instance.
        /// </summary>
        /// <param name="session">The Sharded session on which this query is to be executed.</param>
        /// <param name="queryFactory">Factory method for creation of shard-local <see cref="IQuery"/> instances.</param>
        protected ShardedQueryImpl(IShardedSessionImplementor session, Func<ISession, IQuery> queryFactory)
        {
            Preconditions.CheckNotNull(session);
            Preconditions.CheckNotNull(queryFactory);
            this.session = session;
            this.queryFactory = queryFactory;
        }

        public bool IsReadOnly
        {
            get { return this.SomeQuery.IsReadOnly; }
        }

        /**
         * This method currently wraps list().
         *
         * {@inheritDoc}
         *
         * @return an iterator over the results of the query
         * @throws HibernateException
         */
        public IEnumerable Enumerable()
        {
            return Enumerable<object>();
        }

        public IEnumerable<T> Enumerable<T>()
        {
            return this.session.Execute(new ListShardOperation<T>(this), BuildListExitStrategy<T>());
        }

        /**
         * The implementation executes the query on each shard and concatenates the
         * results.
         *
         * {@inheritDoc}
         *
         * @return a list containing the concatenated results of executing the
         * query on all shards
         * @throws HibernateException
         */
        public IList List()
        {
            return Enumerable<object>().ToList();
        }

        public void List(IList results)
        {
            foreach (var item in Enumerable())
            {
                results.Add(item);
            }
        }

        public IList<T> List<T>()
        {
            return Enumerable<T>().ToList();
        }

        /**
         * The implementation executes the query on each shard and returns the first
         * non-null result.
         *
         * {@inheritDoc}
         *
         * @return the first non-null result, or null if no non-null result found
         * @throws HibernateException
         */
        public object UniqueResult()
        {
            return UniqueResult<object>();

        }

        public T UniqueResult<T>()
        {
            return this.session.Execute(new UniqueResultShardOperation<T>(this), new UniqueResultExitStrategy<T>());
        }

        public IEnumerable<T> Future<T>()
        {
            return this.session.Execute(new FutureShardOperation<T>(this), BuildListExitStrategy<T>());
        }

        public IFutureValue<T> FutureValue<T>()
        {
            return new FutureValueShardOperation<T>(this);
        }

        public int ExecuteUpdate()
        {
            return this.session.Execute(new ExecuteUpdateShardOperation(this), new ExecuteUpdateExitStrategy());
        }

        public IQuery SetMaxResults(int maxResults)
        {
            this.listExitOperationBuilder.MaxResults = maxResults;
            return this;
        }

        public IQuery SetFirstResult(int firstResult)
        {
            this.listExitOperationBuilder.FirstResult = firstResult;
            return this;
        }

        public IQuery SetReadOnly(bool readOnly)
        {
            ApplyActionToShards(q => q.SetReadOnly(readOnly));
            return this;
        }

        public IQuery SetCacheable(bool cacheable)
        {
            ApplyActionToShards(q => q.SetCacheable(cacheable));
            return this;
        }

        public IQuery SetCacheRegion(string cacheRegion)
        {
            ApplyActionToShards(q => q.SetCacheRegion(cacheRegion));
            return this;
        }

        public IQuery SetTimeout(int timeout)
        {
            ApplyActionToShards(q => q.SetTimeout(timeout));
            return this;
        }

        public IQuery SetFetchSize(int fetchSize)
        {
            ApplyActionToShards(q => q.SetFetchSize(fetchSize));
            return this;
        }

        public IQuery SetLockMode(string alias, LockMode lockMode)
        {
            ApplyActionToShards(q => q.SetLockMode(alias, lockMode));
            return this;
        }

        public IQuery SetComment(string comment)
        {
            ApplyActionToShards(q => q.SetComment(comment));
            return this;
        }

        public IQuery SetFlushMode(FlushMode flushMode)
        {
            ApplyActionToShards(q => q.SetFlushMode(flushMode));
            return this;
        }

        public IQuery SetCacheMode(CacheMode cacheMode)
        {
            ApplyActionToShards(q => q.SetCacheMode(cacheMode));
            return this;
        }

        public IQuery SetParameter(int position, object val, IType type)
        {
            ApplyActionToShards(q => q.SetParameter(position, val, type));
            return this;
        }

        public IQuery SetParameter(string name, object val, IType type)
        {
            ApplyActionToShards(q => q.SetParameter(name, val, type));
            return this;
        }

        public IQuery SetParameter<T>(int position, T val)
        {
            ApplyActionToShards(q => q.SetParameter(position, val));
            return this;
        }

        public IQuery SetParameter<T>(string name, T val)
        {
            ApplyActionToShards(q => q.SetParameter(name, val));
            return this;
        }

        public IQuery SetParameter(int position, object val)
        {
            ApplyActionToShards(q => q.SetParameter(position, val));
            return this;
        }

        public IQuery SetParameter(string name, object val)
        {
            ApplyActionToShards(q => q.SetParameter(name, val));
            return this;
        }

        public IQuery SetParameterList(string name, ICollection vals, IType type)
        {
            ApplyActionToShards(q => q.SetParameterList(name, vals, type));
            return this;
        }

        public IQuery SetParameterList(string name, ICollection vals)
        {
            ApplyActionToShards(q => q.SetParameterList(name, vals));
            return this;
        }

        public IQuery SetParameterList(string name, IEnumerable vals, IType type)
        // public IQuery SetParameterList(string name, object[] vals, IType type)
        {
            ApplyActionToShards(q => q.SetParameterList(name, vals, type));
            return this;
        }

        public IQuery SetParameterList(string name, IEnumerable vals)
        //public IQuery SetParameterList(string name, object[] vals)
        {
            ApplyActionToShards(q => q.SetParameterList(name, vals));
            return this;
        }

        public IQuery SetProperties(object obj)
        {
            ApplyActionToShards(q => q.SetProperties(obj));
            return this;
        }

        public IQuery SetAnsiString(int position, string val)
        {
            ApplyActionToShards(q => q.SetAnsiString(position, val));
            return this;
        }

        public IQuery SetAnsiString(string name, string val)
        {
            ApplyActionToShards(q => q.SetAnsiString(name, val));
            return this;
        }

        public IQuery SetBinary(int position, byte[] val)
        {
            ApplyActionToShards(q => q.SetBinary(position, val));
            return this;
        }

        public IQuery SetBinary(string name, byte[] val)
        {
            ApplyActionToShards(q => q.SetBinary(name, val));
            return this;
        }

        public IQuery SetBoolean(int position, bool val)
        {
            ApplyActionToShards(q => q.SetBoolean(position, val));
            return this;
        }

        public IQuery SetBoolean(string name, bool val)
        {
            ApplyActionToShards(q => q.SetBoolean(name, val));
            return this;
        }

        public IQuery SetByte(int position, byte val)
        {
            ApplyActionToShards(q => q.SetByte(position, val));
            return this;
        }

        public IQuery SetByte(string name, byte val)
        {
            ApplyActionToShards(q => q.SetByte(name, val));
            return this;
        }

        public IQuery SetCharacter(int position, char val)
        {
            ApplyActionToShards(q => q.SetCharacter(position, val));
            return this;
        }

        public IQuery SetCharacter(string name, char val)
        {
            ApplyActionToShards(q => q.SetCharacter(name, val));
            return this;
        }

        public IQuery SetDateTime(int position, DateTime val)
        {
            ApplyActionToShards(q => q.SetDateTime(position, val));
            return this;
        }

        public IQuery SetDateTime(string name, DateTime val)
        {
            ApplyActionToShards(q => q.SetDateTime(name, val));
            return this;
        }

        public IQuery SetDecimal(int position, decimal val)
        {
            ApplyActionToShards(q => q.SetDecimal(position, val));
            return this;
        }

        public IQuery SetDecimal(string name, decimal val)
        {
            ApplyActionToShards(q => q.SetDecimal(name, val));
            return this;
        }

        public IQuery SetDouble(int position, double val)
        {
            ApplyActionToShards(q => q.SetDouble(position, val));
            return this;
        }

        public IQuery SetDouble(string name, double val)
        {
            ApplyActionToShards(q => q.SetDouble(name, val));
            return this;
        }

        public IQuery SetEnum(int position, Enum val)
        {
            ApplyActionToShards(q => q.SetEnum(position, val));
            return this;
        }

        public IQuery SetEnum(string name, Enum val)
        {
            ApplyActionToShards(q => q.SetEnum(name, val));
            return this;
        }

        public IQuery SetInt16(int position, short val)
        {
            ApplyActionToShards(q => q.SetInt16(position, val));
            return this;
        }

        public IQuery SetInt16(string name, short val)
        {
            ApplyActionToShards(q => q.SetInt16(name, val));
            return this;
        }

        public IQuery SetInt32(int position, int val)
        {
            ApplyActionToShards(q => q.SetInt32(position, val));
            return this;
        }

        public IQuery SetInt32(string name, int val)
        {
            ApplyActionToShards(q => q.SetInt32(name, val));
            return this;
        }

        public IQuery SetInt64(int position, long val)
        {
            ApplyActionToShards(q => q.SetInt64(position, val));
            return this;
        }

        public IQuery SetInt64(string name, long val)
        {
            ApplyActionToShards(q => q.SetInt64(name, val));
            return this;
        }

        public IQuery SetSingle(int position, float val)
        {
            ApplyActionToShards(q => q.SetSingle(position, val));
            return this;
        }

        public IQuery SetSingle(string name, float val)
        {
            ApplyActionToShards(q => q.SetSingle(name, val));
            return this;
        }

        public IQuery SetString(int position, string val)
        {
            ApplyActionToShards(q => q.SetString(position, val));
            return this;
        }

        public IQuery SetString(string name, string val)
        {
            ApplyActionToShards(q => q.SetString(name, val));
            return this;
        }

        public IQuery SetTime(int position, DateTime val)
        {
            ApplyActionToShards(q => q.SetTime(position, val));
            return this;
        }

        public IQuery SetTime(string name, DateTime val)
        {
            ApplyActionToShards(q => q.SetTime(name, val));
            return this;
        }

        public IQuery SetTimestamp(int position, DateTime val)
        {
            ApplyActionToShards(q => q.SetTimestamp(position, val));
            return this;
        }

        public IQuery SetTimestamp(string name, DateTime val)
        {
            ApplyActionToShards(q => q.SetTimestamp(name, val));
            return this;
        }

        public IQuery SetGuid(int position, Guid val)
        {
            ApplyActionToShards(q => q.SetGuid(position, val));
            return this;
        }

        public IQuery SetGuid(string name, Guid val)
        {
            ApplyActionToShards(q => q.SetGuid(name, val));
            return this;
        }

        public IQuery SetDateTime2(string name, DateTime val)
        {
            ApplyActionToShards(q => q.SetDateTime2(name, val));
            return this;
        }

        public IQuery SetDateTime2(int position, DateTime val)
        {
            ApplyActionToShards(q => q.SetDateTime2(position, val));
            return this;
        }

        public IQuery SetDateTimeOffset(string name, DateTimeOffset val)
        {
            ApplyActionToShards(q => q.SetDateTimeOffset(name, val));
            return this;
        }

        public IQuery SetDateTimeOffset(int position, DateTimeOffset val)
        {
            ApplyActionToShards(q => q.SetDateTimeOffset(position, val));
            return this;
        }

        public IQuery SetTimeAsTimeSpan(string name, TimeSpan val)
        {
            ApplyActionToShards(q => q.SetTimeAsTimeSpan(name, val));
            return this;
        }

        public IQuery SetTimeAsTimeSpan(int position, TimeSpan val)
        {
            ApplyActionToShards(q => q.SetTimeAsTimeSpan(position, val));
            return this;
        }

        public IQuery SetTimeSpan(string name, TimeSpan val)
        {
            ApplyActionToShards(q => q.SetTimeSpan(name, val));
            return this;
        }

        public IQuery SetTimeSpan(int position, TimeSpan val)
        {
            ApplyActionToShards(q => q.SetTimeSpan(position, val));
            return this;
        }

        public IQuery SetEntity(int position, object val)
        {
            ApplyActionToShards(q => q.SetEntity(position, val));
            return this;
        }

        public IQuery SetEntity(string name, object val)
        {
            ApplyActionToShards(q => q.SetEntity(name, val));
            return this;
        }

        public IQuery SetResultTransformer(IResultTransformer resultTransformer)
        {
            ApplyActionToShards(q => q.SetResultTransformer(resultTransformer));
            return this;
        }

        protected void ApplyActionToShards(Action<IQuery> action)
        {
            establishActions.Add(action);
            foreach (var query in this.establishedQueriesByShard.Values)
            {
                action(query);
            }
        }

        private IQuery SomeQuery
        {
            get
            {
                return this.establishedQueriesByShard.Values.FirstOrDefault()
                    ?? EstablishFor(session.AnyShard);
            }
        }

        public string QueryString
        {
            get { return SomeQuery.QueryString; }
        }

        public IType[] ReturnTypes
        {
            get { return SomeQuery.ReturnTypes; }
        }

        public string[] ReturnAliases
        {
            get { return SomeQuery.ReturnAliases; }
        }

        public string[] NamedParameters
        {
            get { return SomeQuery.NamedParameters; }
        }

        public IQuery EstablishFor(IShard shard)
        {
            IQuery result;
            if (!establishedQueriesByShard.TryGetValue(shard, out result))
            {
                result = this.queryFactory(shard.EstablishSession());
                foreach (var action in establishActions)
                {
                    action(result);
                }
                establishedQueriesByShard.Add(shard, result);
            }
            return result;
        }

        public IListExitStrategy<T> BuildListExitStrategy<T>()
        {
            return new ListExitStrategy<T>(this.listExitOperationBuilder.BuildListOperation());
        }

        private class UniqueResultShardOperation<T> : IShardOperation<T>
        {
            private readonly IShardedQuery shardedQuery;

            public UniqueResultShardOperation(IShardedQuery shardedQuery)
            {
                this.shardedQuery = shardedQuery;
            }

            public T Execute(IShard shard)
            {
                return this.shardedQuery.EstablishFor(shard).UniqueResult<T>();
            }

            public string OperationName
            {
                get { return "UniqueResult()"; }
            }
        }

        private class ListShardOperation<T> : IShardOperation<IEnumerable<T>>
        {
            private readonly IShardedQuery shardedQuery;

            public ListShardOperation(IShardedQuery shardedQuery)
            {
                this.shardedQuery = shardedQuery;
            }

            public IEnumerable<T> Execute(IShard shard)
            {
                return this.shardedQuery.EstablishFor(shard).List<T>();
            }

            public string OperationName
            {
                get { return "List()"; }
            }
        }

        private class FutureShardOperation<T> : IShardOperation<IEnumerable<T>>
        {
            private readonly IDictionary<IShard, IEnumerable<T>> futuresByShard;

            public FutureShardOperation(ShardedQueryImpl shardedQuery)
            {
                this.futuresByShard = shardedQuery.session.Shards
                    .ToDictionary(s => s, s => shardedQuery.EstablishFor(s).Future<T>());
            }

            public IEnumerable<T> Execute(IShard shard)
            {
                return this.futuresByShard[shard];
            }

            public string OperationName
            {
                get { return "Future()"; }
            }
        }

        private class FutureValueShardOperation<T> : IShardOperation<T>, IFutureValue<T>
        {
            private readonly IShardedSessionImplementor session;
            private readonly IDictionary<IShard, IFutureValue<T>> futuresByShard;

            public FutureValueShardOperation(ShardedQueryImpl shardedQuery)
            {
                this.session = shardedQuery.session;
                this.futuresByShard = shardedQuery.session.Shards
                    .ToDictionary(s => s, s => shardedQuery.EstablishFor(s).FutureValue<T>());
            }

            public T Execute(IShard shard)
            {
                return this.futuresByShard[shard].Value;
            }

            public T Value
            {
                get { return this.session.Execute(this, new UniqueResultExitStrategy<T>()); }
            }

            public string OperationName
            {
                get { return "FutureValue()"; }
            }
        }

        private class ExecuteUpdateShardOperation : IShardOperation<int>
        {
            private readonly IShardedQuery shardedQuery;

            public ExecuteUpdateShardOperation(IShardedQuery shardedQuery)
            {
                this.shardedQuery = shardedQuery;
            }

            public int Execute(IShard shard)
            {
                return this.shardedQuery.EstablishFor(shard).ExecuteUpdate();
            }

            public string OperationName
            {
                get { return "ExecuteUpdate()"; }
            }
        }
    }
}