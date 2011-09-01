using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Transform;
using NHibernate.Type;

namespace NHibernate.Shards.Query
{
    public class ShardedMultiQueryImpl: IShardedMultiQuery
    {
        #region Instance fields

        private readonly IShardedSessionImplementor session;

        private readonly IDictionary<IShard, IMultiQuery> establishedMultiQueriesByShard = new Dictionary<IShard, IMultiQuery>();
        private readonly ICollection<Action<IMultiQuery>> establishActions = new List<Action<IMultiQuery>>();
        private readonly IList<QueryEntry> entries = new List<QueryEntry>();

        private IList queryResult;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Creates new <see cref="ShardedQueryImpl"/> instance.
        /// </summary>
        /// <param name="session">The Sharded session on which this query is to be executed.</param>
        public ShardedMultiQueryImpl(IShardedSessionImplementor session)
		{
            this.session = session;
        }

        #endregion

        #region IMultiQuery Members

        public IMultiQuery Add(string hql)
        {
            AddEntry(new QueryEntry(ShardedQueryImpl.CreateQuery(this.session, hql), () => new ArrayList()));
            return this;
        }

        public IMultiQuery Add(string key, string hql)
        {
            AddEntry(new QueryEntry(key, ShardedQueryImpl.CreateQuery(this.session, hql), () => new ArrayList()));
            return this;
        }

        public IMultiQuery Add<T>(string hql)
        {
            AddEntry(new QueryEntry(ShardedQueryImpl.CreateQuery(this.session, hql), () => new List<T>()));
            return this;
        }

        public IMultiQuery Add<T>(string key, string hql)
        {
            AddEntry(new QueryEntry(key, ShardedQueryImpl.CreateQuery(this.session, hql), () => new List<T>()));
            return this;
        }

        public IMultiQuery Add(IQuery query)
        {
            AddEntry(new QueryEntry(query, () => new ArrayList()));
            return this;
        }

        public IMultiQuery Add(string key, IQuery query)
        {
            AddEntry(new QueryEntry(key, query, () => new ArrayList()));
            return this;
        }

        public IMultiQuery Add<T>(IQuery query)
        {
            AddEntry(new QueryEntry(query, () => new ArrayList()));
            return this;
        }

        public IMultiQuery Add<T>(string key, IQuery query)
        {
            AddEntry(new QueryEntry(key, query, () => new ArrayList()));
            return this;
        }

        public IMultiQuery Add(System.Type resultGenericListType, IQuery query)
        {
            var listType = typeof(IList<>).MakeGenericType(resultGenericListType);
            AddEntry(new QueryEntry(query, () => (IList)Activator.CreateInstance(listType)));
            return this;
        }

        public IMultiQuery AddNamedQuery(string queryName)
        {
            AddEntry(new QueryEntry(ShardedQueryImpl.GetNamedQuery(this.session, queryName), () => new ArrayList()));
            return this;
        }

        public IMultiQuery AddNamedQuery(string key, string queryName)
        {
            AddEntry(new QueryEntry(key, ShardedQueryImpl.GetNamedQuery(this.session, queryName), () => new ArrayList()));
            return this;
        }

        public IMultiQuery AddNamedQuery<T>(string queryName)
        {
            AddEntry(new QueryEntry(ShardedQueryImpl.GetNamedQuery(this.session, queryName), () => new List<T>()));
            return this;
        }

        public IMultiQuery AddNamedQuery<T>(string key, string queryName)
        {
            AddEntry(new QueryEntry(key, ShardedQueryImpl.GetNamedQuery(this.session, queryName), () => new List<T>()));
            return this;
        }

        private static IShardedQuery ToShardedQuery(IQuery query)
        {
            var shardedQuery = query as IShardedQuery;
            if (shardedQuery == null)
            {
                throw new ArgumentException("Query must be a sharded query.", "query");
            }
            return shardedQuery;
        }

        public object GetResult(string key)
        {
            if (this.queryResult == null)
            {
                this.queryResult = List();
            }

            for(int i = 0; i < this.entries.Count; i++)
            {
                if (this.entries[i].Key == key) return this.queryResult[i];
            }

            throw new KeyNotFoundException();
        }

        public IList List()
        {
            var exitStrategies = this.entries.Select(i => i.BuildListExitStrategy());
            var result = this.session.Execute(new ListShardOperation(this), new MultiExitStrategy(exitStrategies));

            var resultLists = new IList[this.entries.Count];
            for (int i = 0; i < this.entries.Count; i++)
            {
                resultLists[i] = this.entries[i].BuildResultList((IEnumerable)result[i]);
            }
            return resultLists;
        }

        public IMultiQuery SetAnsiString(string name, string val)
        {
            ApplyActionToShards(q => q.SetAnsiString(name, val));
            return this;
        }

        public IMultiQuery SetBinary(string name, byte[] val)
        {
            ApplyActionToShards(q => q.SetBinary(name, val));
            return this;
        }

        public IMultiQuery SetBoolean(string name, bool val)
        {
            ApplyActionToShards(q => q.SetBoolean(name, val));
            return this;
        }

        public IMultiQuery SetByte(string name, byte val)
        {
            ApplyActionToShards(q => q.SetByte(name, val));
            return this;
        }

        public IMultiQuery SetCacheRegion(string region)
        {
            ApplyActionToShards(q => q.SetCacheRegion(region));
            return this;
        }

        public IMultiQuery SetCacheable(bool cacheable)
        {
            ApplyActionToShards(q => q.SetCacheable(cacheable));
            return this;
        }

        public IMultiQuery SetCharacter(string name, char val)
        {
            ApplyActionToShards(q => q.SetCharacter(name, val));
            return this;
        }

        public IMultiQuery SetDateTime(string name, DateTime val)
        {
            ApplyActionToShards(q => q.SetDateTime(name, val));
            return this;
        }

        public IMultiQuery SetDecimal(string name, decimal val)
        {
            ApplyActionToShards(q => q.SetDecimal(name, val));
            return this;
        }

        public IMultiQuery SetDouble(string name, double val)
        {
            ApplyActionToShards(q => q.SetDouble(name, val));
            return this;
        }

        public IMultiQuery SetEntity(string name, object val)
        {
            ApplyActionToShards(q => q.SetEntity(name, val));
            return this;
        }

        public IMultiQuery SetEnum(string name, Enum val)
        {
            ApplyActionToShards(q => q.SetEnum(name, val));
            return this;
        }

        public IMultiQuery SetFlushMode(FlushMode mode)
        {
            ApplyActionToShards(q => q.SetFlushMode(mode));
            return this;
        }

        public IMultiQuery SetForceCacheRefresh(bool forceCacheRefresh)
        {
            ApplyActionToShards(q => q.SetForceCacheRefresh(forceCacheRefresh));
            return this;
        }

        public IMultiQuery SetGuid(string name, Guid val)
        {
            ApplyActionToShards(q => q.SetGuid(name, val));
            return this;
        }

        public IMultiQuery SetInt16(string name, short val)
        {
            ApplyActionToShards(q => q.SetInt16(name, val));
            return this;
        }

        public IMultiQuery SetInt32(string name, int val)
        {
            ApplyActionToShards(q => q.SetInt32(name, val));
            return this;
        }

        public IMultiQuery SetInt64(string name, long val)
        {
            ApplyActionToShards(q => q.SetInt64(name, val));
            return this;
        }

        public IMultiQuery SetDateTime2(string name, DateTime val)
        {
            ApplyActionToShards(q => q.SetDateTime2(name, val));
            return this;
        }

        public IMultiQuery SetDateTimeOffset(string name, DateTimeOffset val)
        {
            ApplyActionToShards(q => q.SetDateTimeOffset(name, val));
            return this;
        }

        public IMultiQuery SetTimeAsTimeSpan(string name, TimeSpan val)
        {
            ApplyActionToShards(q => q.SetTimeAsTimeSpan(name, val));
            return this;
        }

        public IMultiQuery SetTimeSpan(string name, TimeSpan val)
        {
            ApplyActionToShards(q => q.SetTimeSpan(name, val));
            return this;
        }

        public IMultiQuery SetParameter(string name, object val)
        {
            ApplyActionToShards(q => q.SetParameter(name, val));
            return this;
        }

        public IMultiQuery SetParameter(string name, object val, IType type)
        {
            ApplyActionToShards(q => q.SetParameter(name, val, type));
            return this;
        }

        public IMultiQuery SetParameterList(string name, ICollection vals)
        {
            ApplyActionToShards(q => q.SetParameterList(name, vals));
            return this;
        }

        public IMultiQuery SetParameterList(string name, ICollection vals, IType type)
        {
            ApplyActionToShards(q => q.SetParameterList(name, vals, type));
            return this;
        }

        public IMultiQuery SetResultTransformer(IResultTransformer transformer)
        {
            ApplyActionToShards(q => q.SetResultTransformer(transformer));
            return this;
        }

        public IMultiQuery SetSingle(string name, float val)
        {
            ApplyActionToShards(q => q.SetSingle(name, val));
            return this;
        }

        public IMultiQuery SetString(string name, string val)
        {
            ApplyActionToShards(q => q.SetString(name, val));
            return this;
        }

        public IMultiQuery SetTime(string name, DateTime val)
        {
            ApplyActionToShards(q => q.SetTime(name, val));
            return this;
        }

        public IMultiQuery SetTimeout(int timeout)
        {
            ApplyActionToShards(q => q.SetTimeout(timeout));
            return this;
        }

        public IMultiQuery SetTimestamp(string name, DateTime val)
        {
            ApplyActionToShards(q => q.SetTimestamp(name, val));
            return this;
        }

        public IMultiQuery EstablishFor(IShard shard)
        {
            IMultiQuery multiQuery;
            if (!this.establishedMultiQueriesByShard.TryGetValue(shard, out multiQuery))
            {
                multiQuery = shard.EstablishSession().CreateMultiQuery();
                
                foreach (var entry in this.entries)
                {
                    multiQuery.Add(entry.ShardedQuery.EstablishFor(shard));
                }
                
                foreach (var action in establishActions)
                {
                    action(multiQuery);
                }
                
                this.establishedMultiQueriesByShard.Add(shard, multiQuery);
            }
            return multiQuery;
        }

        private void AddEntry(QueryEntry entry)
        {
            entries.Add(entry);

            foreach (var pair in this.establishedMultiQueriesByShard)
            {
                pair.Value.Add(entry.ShardedQuery.EstablishFor(pair.Key));
            }
        }

        private void ApplyActionToShards(Action<IMultiQuery> action)
        {
            establishActions.Add(action);
            foreach (var multiQuery in this.establishedMultiQueriesByShard.Values)
            {
                action(multiQuery);
            }
        }

        #endregion

        #region Inner classes

        private class QueryEntry
        {
            public readonly string Key;
            public readonly IShardedQuery ShardedQuery;
            private readonly Func<IList> resultFactory;

            public QueryEntry(IQuery query, Func<IList> resultFactory)
                : this(null, query, resultFactory)
            {}

            public QueryEntry(string key, IQuery query, Func<IList> resultFactory)
            {
                this.Key = key;
                this.ShardedQuery = ToShardedQuery(query);
                this.resultFactory = resultFactory;
            }

            public IListExitStrategy<object> BuildListExitStrategy()
            {
                return this.ShardedQuery.BuildListExitStrategy<object>();
            }

            public IList BuildResultList(IEnumerable result)
            {
                var list = this.resultFactory();
                foreach (var item in result)
                {
                    list.Add(item);
                }
                return list;
            }
        }

        private class ListShardOperation : IShardOperation<IList>
        {
            private readonly IShardedMultiQuery shardedMultiQuery;

            public ListShardOperation(IShardedMultiQuery shardedMultiQuery)
            {
                this.shardedMultiQuery = shardedMultiQuery;
            }

            public IList Execute(IShard shard)
            {
                return this.shardedMultiQuery.EstablishFor(shard).List();
            }

            public string OperationName
            {
                get { return "List()"; }
            }
        }

        #endregion
    }
}
