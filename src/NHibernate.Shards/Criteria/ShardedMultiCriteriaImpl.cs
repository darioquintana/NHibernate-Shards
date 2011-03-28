using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Criterion;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Transform;

namespace NHibernate.Shards.Criteria
{
    public class ShardedMultiCriteriaImpl: IShardedMultiCriteria
    {
        #region Instance fields

        private readonly IShardedSessionImplementor session;
        private readonly IList<CriteriaEntry> entries = new List<CriteriaEntry>();

        private readonly IDictionary<IShard, IMultiCriteria> establishedMultiCriteriaByShard = new Dictionary<IShard, IMultiCriteria>();
        private readonly ICollection<Action<IMultiCriteria>> establishActions = new List<Action<IMultiCriteria>>();

        private IList criteriaResult;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Creates new <see cref="ShardedCriteriaImpl"/> instance.
        /// </summary>
        /// <param name="session">The Sharded session on which this query is to be executed.</param>
        public ShardedMultiCriteriaImpl(IShardedSessionImplementor session)
		{
			this.session = session;
        }

        #endregion

        #region IMultiCriteria implementation

        public object GetResult(string key)
        {
            if (this.criteriaResult == null)
            {
                this.criteriaResult = List();
            }

            for (int i = 0; i < this.entries.Count; i++)
            {
                if (this.entries[i].Key == key) return this.criteriaResult[i];
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

        public IMultiCriteria Add(System.Type resultGenericListType, ICriteria criteria)
        {
            var listType = typeof(IList<>).MakeGenericType(resultGenericListType);
            Add(ToShardedCriteria(criteria), () => (IList)Activator.CreateInstance(listType));
            return this;
        }

        public IMultiCriteria Add<T>(ICriteria criteria)
        {
            Add(ToShardedCriteria(criteria), () => new List<T>());
            return this;
        }

        public IMultiCriteria Add<T>(string key, ICriteria criteria)
        {
            Add(key, ToShardedCriteria(criteria), () => new List<T>());
            return this;
        }

        public IMultiCriteria Add<T>(DetachedCriteria detachedCriteria)
        {
            throw DetachedCriteriaNotSupportedException();
        }

        public IMultiCriteria Add<T>(string key, DetachedCriteria detachedCriteria)
        {
            throw DetachedCriteriaNotSupportedException();
        }

        private static Exception DetachedCriteriaNotSupportedException()
        {
            // DetachedCriteria.GetExecutableCriteria(session) will fail because
            // it tries to map the session to an ISessionImplementor interface. This 
            // interface is near impossible to implement for sharded sessions. 
            return new NotSupportedException("Detached criteria are not (yet) supported by sharded multi criteria.");
        }

        public IMultiCriteria Add(ICriteria criteria)
        {
            return Add<object>(criteria);
        }

        public IMultiCriteria Add(string key, ICriteria criteria)
        {
            return Add<object>(key, criteria);
        }

        public IMultiCriteria Add(DetachedCriteria detachedCriteria)
        {
            return Add<object>(detachedCriteria);
        }

        public IMultiCriteria Add(string key, DetachedCriteria detachedCriteria)
        {
            return Add<object>(key, detachedCriteria);
        }

        public IMultiCriteria Add(System.Type resultGenericListType, IQueryOver queryOver)
        {
            throw QueryOverNotSupportedException();
        }

        public IMultiCriteria Add<T>(IQueryOver<T> queryOver)
        {
            throw QueryOverNotSupportedException();
        }

        public IMultiCriteria Add<U>(IQueryOver queryOver)
        {
            throw QueryOverNotSupportedException();
        }

        public IMultiCriteria Add<T>(string key, IQueryOver<T> queryOver)
        {
            throw QueryOverNotSupportedException();
        }

        public IMultiCriteria Add<U>(string key, IQueryOver queryOver)
        {
            throw QueryOverNotSupportedException();
        }

        private static Exception QueryOverNotSupportedException()
        {
            // IQueryOver<,>.GetExecutableCriteria(session) will fail because
            // it tries to map the session to an ISessionImplementor interface. This 
            // interface is near impossible to implement for sharded sessions. 
            return new NotSupportedException("QuerOver is not (yet) supported by sharded multi criteria.");
        }

        private static IShardedCriteria ToShardedCriteria(ICriteria criteria)
        {
            var shardedCriteria = criteria as IShardedCriteria;
            if (shardedCriteria == null)
            {
                throw new ArgumentException("Criteria must be a sharded criteria.", "criteria");
            }
            return shardedCriteria;
        }

        private void Add(IShardedCriteria shardedCriteria, Func<IList> resultFactory)
        {
            entries.Add(new CriteriaEntry(null, shardedCriteria, resultFactory));

            foreach (var pair in this.establishedMultiCriteriaByShard)
            {
                pair.Value.Add(shardedCriteria.EstablishFor(pair.Key));
            }
        }

        private void Add(string key, IShardedCriteria shardedCriteria, Func<IList> resultFactory)
        {
            entries.Add(new CriteriaEntry(key, shardedCriteria, resultFactory));

            foreach (var pair in this.establishedMultiCriteriaByShard)
            {
                pair.Value.Add(key, shardedCriteria.EstablishFor(pair.Key));
            }
        }

        public IMultiCriteria SetCacheable(bool cachable)
        {
            ApplyActionToShards(c => c.SetCacheable(cachable));
            return this;
        }

        public IMultiCriteria SetCacheRegion(string region)
        {
            ApplyActionToShards(c => c.SetCacheRegion(region));
            return this;
        }

        public IMultiCriteria ForceCacheRefresh(bool forceRefresh)
        {
            ApplyActionToShards(c => c.ForceCacheRefresh(forceRefresh));
            return this;
        }

        public IMultiCriteria SetResultTransformer(IResultTransformer resultTransformer)
        {
            ApplyActionToShards(c => c.SetResultTransformer(resultTransformer));
            return this;
        }

        public IMultiCriteria EstablishFor(IShard shard)
        {
            IMultiCriteria multiCriteria;
            if (!this.establishedMultiCriteriaByShard.TryGetValue(shard, out multiCriteria))
            {
                multiCriteria = shard.EstablishSession().CreateMultiCriteria();
                foreach (var entry in this.entries)
                {
                    multiCriteria.Add(entry.ShardedCriteria.EstablishFor(shard));
                }
                foreach (var action in establishActions)
                {
                    action(multiCriteria);
                }
                this.establishedMultiCriteriaByShard.Add(shard, multiCriteria);
            }
            return multiCriteria;
        }

        private void ApplyActionToShards(Action<IMultiCriteria> action)
        {
            establishActions.Add(action);
            foreach (var multiCriteria in this.establishedMultiCriteriaByShard.Values)
            {
                action(multiCriteria);
            }
        }

        #endregion

        #region Inner classes

        private class CriteriaEntry
        {
            public readonly string Key;
            public readonly IShardedCriteria ShardedCriteria;
            private readonly Func<IList> ResultFactory;

            public CriteriaEntry(string key, IShardedCriteria shardedCriteria, Func<IList> resultFactory)
            {
                this.Key = key;
                this.ShardedCriteria = shardedCriteria;
                this.ResultFactory = resultFactory;
            }

            public IListExitStrategy<object> BuildListExitStrategy()
            {
                return this.ShardedCriteria.BuildListExitStrategy<object>();
            }

            public IList BuildResultList(IEnumerable result)
            {
                var list = this.ResultFactory();
                foreach (var item in result)
                {
                    list.Add(item);
                }
                return list;
            }
        }

        private class ListShardOperation : IShardOperation<IList>
        {
            private readonly IShardedMultiCriteria shardedMultiCriteria;

            public ListShardOperation(IShardedMultiCriteria shardedMultiCriteria)
            {
                this.shardedMultiCriteria = shardedMultiCriteria;
            }

            public IList Execute(IShard shard)
            {
                return this.shardedMultiCriteria.EstablishFor(shard).List();
            }

            public string OperationName
            {
                get { return "List()"; }
            }
        }

        #endregion
    }
}
