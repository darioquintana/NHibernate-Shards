using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NHibernate.Criterion;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Util;
using NHibernate.SqlCommand;
using NHibernate.Transform;

namespace NHibernate.Shards.Criteria
{
    /// <summary>
    /// Concrete implementation of <see cref="IShardedCriteria"/> interface.
    /// </summary>
    public class ShardedCriteriaImpl : IShardedCriteria
    {
        private static readonly IInternalLogger Log = LoggerProvider.LoggerFor(typeof(ShardedCriteriaImpl));

        #region Instance fields

        private readonly IShardedSessionImplementor session;
        private readonly Func<ISession, ICriteria> criteriaFactory;
        private readonly ListExitOperationBuilder listExitOperationBuilder = new ListExitOperationBuilder();

        private readonly IDictionary<IShard, ICriteria> establishedCriteriaByShard = new Dictionary<IShard, ICriteria>();
        private readonly ICollection<Action<ICriteria>> establishActions = new List<Action<ICriteria>>();

        private readonly IDictionary<string, ICriteria> subcriteriaByAlias = new Dictionary<string, ICriteria>();
        private readonly IDictionary<string, Subcriteria> subcriteriaByPath = new Dictionary<string, Subcriteria>();

        #endregion

        #region Constructor(s)

        public ShardedCriteriaImpl(IShardedSessionImplementor session, Func<ISession, ICriteria> criteriaFactory)
        {
            Preconditions.CheckNotNull(session);
            Preconditions.CheckNotNull(criteriaFactory);
            this.session = session;
            this.criteriaFactory = criteriaFactory;
            this.subcriteriaByAlias[CriteriaSpecification.RootAlias] = this;
        }

        #endregion

        #region Properties

        private ICriteria SomeCriteria
        {
            get
            {
                return this.establishedCriteriaByShard.Values.FirstOrDefault()
                    ?? EstablishFor(this.session.AnyShard);
            }
        }

        #endregion

        #region ICriteria implementation

        public bool IsReadOnly
        {
            get { return this.SomeCriteria.IsReadOnly; }
        }

        public bool IsReadOnlyInitialized
        {
            get { return this.SomeCriteria.IsReadOnlyInitialized; }
        }

        public ICriteria SetReadOnly(bool readOnly)
        {
            ApplyActionToShards(c => c.SetReadOnly(readOnly));
            return this;
        }

        public ICriteria SetProjection(params IProjection[] projections)
        {
            foreach (var projection in projections)
            {
                SetProjection(projection);
            }
            return this;
        }

        public ICriteria SetProjection(IProjection projection)
        {
            if (projection is ProjectionList)
            {
                throw new NotSupportedException("Projection lists are not (yet) supported.");
            }

            var distinct = projection as Distinct;
            if (distinct != null)
            {
                ApplyActionToShards(c => c.SetProjection(projection));
                this.listExitOperationBuilder.Distinct = true;
                return this;
            }

            if (!projection.IsAggregate)
            {
                ApplyActionToShards(c => c.SetProjection(projection));
                return this;
            }

            string aggregationName = projection.ToString();
            if (aggregationName.StartsWith("avg", StringComparison.OrdinalIgnoreCase))
            {
                var projectionList = Projections.ProjectionList()
                    .Add(projection)
                    .Add(Projections.RowCount());
                ApplyActionToShards(c => c.SetProjection(projectionList));
                this.listExitOperationBuilder.Aggregation =
                    c => AggregationUtil.Average(c, GetDoubleFieldSelector(0), GetInt32FieldSelector(1));
                return this;
            }

            if (aggregationName.StartsWith("sum", StringComparison.OrdinalIgnoreCase))
            {
                ApplyActionToShards(c => c.SetProjection(projection));
                this.listExitOperationBuilder.Aggregation = ToSumFunc(projection);
                return this;
            }

            if (aggregationName.StartsWith("count", StringComparison.OrdinalIgnoreCase))
            {
                ApplyActionToShards(c => c.SetProjection(projection));
                this.listExitOperationBuilder.Aggregation = ToSumFunc(projection);
                return this;
            }

            if (aggregationName.StartsWith("min", StringComparison.OrdinalIgnoreCase))
            {
                ApplyActionToShards(c => c.SetProjection(projection));
                this.listExitOperationBuilder.Aggregation = AggregationUtil.Min;
                return this;
            }

            if (aggregationName.StartsWith("max", StringComparison.OrdinalIgnoreCase))
            {
                ApplyActionToShards(c => c.SetProjection(projection));
                this.listExitOperationBuilder.Aggregation = AggregationUtil.Max;
                return this;
            }

            var message = string.Format(
                CultureInfo.InvariantCulture,
                "Aggregate projection '{0}' is currently not supported across shards.",
                aggregationName);
            Log.Error(message);
            throw new NotSupportedException(message);
        }

        private static AggregationFunc ToSumFunc(IProjection projection)
        {
            var aggregationResultClass = projection.GetTypes(null, null)[0].ReturnedClass;
            return AggregationUtil.GetSumFunc(aggregationResultClass);
        }

        private static Func<object, double?> GetDoubleFieldSelector(int fieldIndex)
        {
            return o =>
            {
                var value = GetFieldAt(o, fieldIndex);
                return value != null
                    ? Convert.ToDouble(value)
                    : default(double?);
            };
        }

        private static Func<object, int?> GetInt32FieldSelector(int fieldIndex)
        {
            return o =>
            {
                var value = GetFieldAt(o, fieldIndex);
                return value != null
                    ? Convert.ToInt32(value)
                    : default(int?);
            };
        }

        private static object GetFieldAt(object array, int index)
        {
            var values = array as object[];
            return values == null || values.Length <= index
                ? null
                : values[index];
        }

        public ICriteria Add(ICriterion criterion)
        {
            ApplyActionToShards(c => c.Add(criterion));
            return this;
        }

        public ICriteria AddOrder(Order order)
        {
            this.listExitOperationBuilder.Orders.Add(ToSortOrder(order));
            ApplyActionToShards(c => c.AddOrder(order));
            return this;
        }

        public void ClearOrders()
        {
            this.listExitOperationBuilder.Orders.Clear();
            ApplyActionToShards(c => c.ClearOrders());
        }

        private static SortOrder ToSortOrder(Order order)
        {
            var orderClause = order.ToString();
            var spaceIndex = orderClause.LastIndexOf(' ');

            var propertyName = orderClause.Substring(0, spaceIndex);
            var orderDirection = orderClause.Substring(spaceIndex + 1);

            bool isDescending;
            switch (orderDirection)
            {
                case "asc":
                    isDescending = false;
                    break;
                case "desc":
                    isDescending = true;
                    break;
                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Order '{0}' specifies invalid order direction '{1}'.",
                            orderClause, orderDirection),
                        "order");
            }

            return new SortOrder(propertyName, isDescending);
        }

        public ICriteria SetFetchMode(string associationPath, FetchMode fetchMode)
        {
            ApplyActionToShards(c => c.SetFetchMode(associationPath, fetchMode));
            return this;
        }

        public ICriteria SetLockMode(LockMode lockMode)
        {
            ApplyActionToShards(c => c.SetLockMode(lockMode));
            return this;
        }

        public ICriteria SetLockMode(string alias, LockMode lockMode)
        {
            ApplyActionToShards(c => c.SetLockMode(alias, lockMode)); //fixed
            return this;
        }

        public ICriteria CreateAlias(string associationPath, string alias)
        {
            CreateCriteria(associationPath, alias);
            return this;
        }

        public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType)
        {
            CreateCriteria(associationPath, alias, joinType);
            return this;
        }

        public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType, ICriterion withClause)
        {
            CreateCriteria(associationPath, alias, joinType, withClause);
            return this;
        }

        public ICriteria CreateCriteria(string associationPath)
        {
            return CreateSubcriteria(associationPath, null, c => c.CreateCriteria(associationPath));
        }

        public ICriteria CreateCriteria(string associationPath, JoinType joinType)
        {
            return CreateSubcriteria(associationPath, null, c => c.CreateCriteria(associationPath, joinType));
        }

        public ICriteria CreateCriteria(string associationPath, string alias)
        {
            return CreateSubcriteria(associationPath, alias, c => c.CreateCriteria(associationPath, alias));
        }

        public ICriteria CreateCriteria(string associationPath, string alias, JoinType joinType)
        {
            return CreateSubcriteria(associationPath, alias, c => c.CreateCriteria(associationPath, alias, joinType));
        }

        public ICriteria CreateCriteria(string associationPath, string alias, JoinType joinType, ICriterion withClause)
        {
            return CreateSubcriteria(associationPath, alias, c => c.CreateCriteria(associationPath, alias, joinType, withClause));
        }

        private Subcriteria CreateSubcriteria(string associationPath, string alias, Func<ICriteria, ICriteria> subcriteriaFactory)
        {
            var subcriteria = new Subcriteria(this, alias, subcriteriaFactory);

            this.subcriteriaByPath[associationPath] = subcriteria;
            if (!string.IsNullOrEmpty(alias))
            {
                this.subcriteriaByAlias[alias] = subcriteria;
            }

            foreach (var pair in this.establishedCriteriaByShard)
            {
                subcriteria.EstablishFor(pair.Key, pair.Value);
            }
            return subcriteria;
        }

        public ICriteria SetResultTransformer(IResultTransformer resultTransformer)
        {
            // TODO: verify whether this works as intended when aggregate projections have been added, 
            // as they can cause changes to the definition of the underlying ICriteria instances for 
            // each shard.
            ApplyActionToShards(c => c.SetResultTransformer(resultTransformer));
            return this;
        }

        public ICriteria SetMaxResults(int maxResults)
        {
            this.listExitOperationBuilder.MaxResults = maxResults;
            ApplyLimitsToShards();
            return this;
        }

        public ICriteria SetFirstResult(int firstResult)
        {
            this.listExitOperationBuilder.FirstResult = firstResult;
            ApplyLimitsToShards();
            return this;
        }

        private void ApplyLimitsToShards()
        {
            if (this.listExitOperationBuilder.MaxResults.HasValue)
            {
                var maxResults = this.listExitOperationBuilder.MaxResults.Value
                    + this.listExitOperationBuilder.FirstResult;
                ApplyActionToShards(c => c.SetMaxResults(maxResults));
            }
        }

        public ICriteria SetFetchSize(int fetchSize)
        {
            ApplyActionToShards(c => c.SetFetchSize(fetchSize));
            return this;
        }

        public ICriteria SetTimeout(int timeout)
        {
            ApplyActionToShards(c => c.SetTimeout(timeout));
            return this;
        }

        public ICriteria SetCacheable(bool cacheable)
        {
            ApplyActionToShards(c => c.SetCacheable(cacheable));
            return this;
        }

        public ICriteria SetCacheRegion(string cacheRegion)
        {
            ApplyActionToShards(c => c.SetCacheRegion(cacheRegion));
            return this;
        }

        public ICriteria SetComment(string comment)
        {
            ApplyActionToShards(c => c.SetComment(comment));
            return this;
        }

        public ICriteria SetFlushMode(FlushMode flushMode)
        {
            ApplyActionToShards(c => c.SetFlushMode(flushMode));
            return this;
        }

        public ICriteria SetCacheMode(CacheMode cacheMode)
        {
            ApplyActionToShards(c => c.SetCacheMode(cacheMode));
            return this;
        }

        public IList List()
        {
            return this.session
                .Execute(new ListShardOperation<object>(this), BuildListExitStrategy<object>())
                .ToList();
        }

        public IList<T> List<T>()
        {
            return this.session
                .Execute(new ListShardOperation<T>(this), BuildListExitStrategy<T>())
                .ToList();
        }

        public void List(IList results)
        {
            /**
             * We don't support shard selection for criteria queries.  If you want
             * custom shards, create a ShardedSession with only the shards you want.
             * We're going to concatenate all our results and then use our
             * criteria collector to do post processing.
             */
            var items = this.session.Execute(new ListShardOperation<object>(this), BuildListExitStrategy<object>());
            foreach (var item in items)
            {
                results.Add(item);
            }
        }

        public object UniqueResult()
        {
            return UniqueResult<object>();
        }

        public T UniqueResult<T>()
        {
            return this.session.Execute(new UniqueResultOperation<T>(this), new UniqueResultExitStrategy<T>());
        }

        public IEnumerable<T> Future<T>()
        {
            return this.session.Execute(new FutureShardOperation<T>(this), BuildListExitStrategy<T>());
        }

        public IFutureValue<T> FutureValue<T>()
        {
            return new FutureValueShardOperation<T>(this);
        }

        public IListExitStrategy<T> BuildListExitStrategy<T>()
        {
            return new ListExitStrategy<T>(listExitOperationBuilder.BuildListOperation());
        }

        public ICriteria GetCriteriaByPath(string path)
        {
            Subcriteria result;
            return (subcriteriaByPath.TryGetValue(path, out result))
                ? result
                : null;
        }

        public ICriteria GetCriteriaByAlias(string alias)
        {
            ICriteria result;
            return (subcriteriaByAlias.TryGetValue(alias, out result))
                ? result
                : null;
        }

        public System.Type GetRootEntityTypeIfAvailable()
        {
            return SomeCriteria.GetRootEntityTypeIfAvailable();
        }

        public string Alias
        {
            get { return CriteriaSpecification.RootAlias; }
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        protected void ApplyActionToShards(Action<ICriteria> action)
        {
            this.establishActions.Add(action);
            foreach (var criteria in this.establishedCriteriaByShard.Values)
            {
                action(criteria);
            }
        }

        public ICriteria EstablishFor(IShard shard)
        {
            ICriteria result;
            if (!establishedCriteriaByShard.TryGetValue(shard, out result))
            {
                result = this.criteriaFactory(shard.EstablishSession());
                foreach (var subcriteria in this.subcriteriaByPath.Values)
                {
                    subcriteria.EstablishFor(shard, result);
                }
                foreach (Action<ICriteria> action in this.establishActions)
                {
                    action(result);
                }

                establishedCriteriaByShard.Add(shard, result);
            }
            return result;
        }

        #endregion

        #region Inner classes

        private class ListShardOperation<T> : IShardOperation<IEnumerable<T>>
        {
            private readonly IShardedCriteria shardedCriteria;

            public ListShardOperation(IShardedCriteria shardedCriteria)
            {
                this.shardedCriteria = shardedCriteria;
            }

            public IEnumerable<T> Execute(IShard shard)
            {
                return this.shardedCriteria.EstablishFor(shard).List<T>();
            }

            public string OperationName
            {
                get { return "List()"; }
            }
        }

        public class UniqueResultOperation<T> : IShardOperation<T>
        {
            private readonly IShardedCriteria shardedCriteria;

            public UniqueResultOperation(IShardedCriteria shardedCriteria)
            {
                this.shardedCriteria = shardedCriteria;
            }

            public T Execute(IShard shard)
            {
                return shardedCriteria.EstablishFor(shard).UniqueResult<T>();
            }

            public string OperationName
            {
                get { return "uniqueResult()"; }
            }
        }

        private class FutureShardOperation<T> : IShardOperation<IEnumerable<T>>
        {
            private readonly IDictionary<IShard, IEnumerable<T>> futuresByShard;

            public FutureShardOperation(ShardedCriteriaImpl shardedCriteria)
            {
                this.futuresByShard = shardedCriteria.session.Shards
                    .ToDictionary(s => s, s => shardedCriteria.EstablishFor(s).Future<T>());
            }

            public IEnumerable<T> Execute(IShard shard)
            {
                return futuresByShard[shard];
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

            public FutureValueShardOperation(ShardedCriteriaImpl shardedCriteria)
            {
                this.session = shardedCriteria.session;
                this.futuresByShard = shardedCriteria.session.Shards
                    .ToDictionary(s => s, s => shardedCriteria.EstablishFor(s).FutureValue<T>());
            }

            public T Execute(IShard shard)
            {
                return futuresByShard[shard].Value;
            }

            public T Value
            {
                get { return session.Execute(this, new UniqueResultExitStrategy<T>()); }
            }

            public string OperationName
            {
                get { return "FutureValue()"; }
            }
        }

        private class Subcriteria : ICriteria
        {
            private readonly ShardedCriteriaImpl root;
            private readonly string subcriteriaAlias;
            private readonly Func<ICriteria, ICriteria> subcriteriaFactory;
            private readonly IDictionary<IShard, ICriteria> establishedSubcriteriaByShard = new Dictionary<IShard, ICriteria>();
            private readonly ICollection<Action<ICriteria>> establishActions = new List<Action<ICriteria>>();

            public Subcriteria(ShardedCriteriaImpl root, string alias, Func<ICriteria, ICriteria> subcriteriaFactory)
            {
                this.root = root;
                this.subcriteriaAlias = alias;
                this.subcriteriaFactory = subcriteriaFactory;
            }

            public object Clone()
            {
                return root.Clone();
            }

            public T UniqueResult<T>()
            {
                return root.UniqueResult<T>();
            }

            public void ClearOrders()
            {
                root.ClearOrders();
            }

            public IEnumerable<T> Future<T>()
            {
                return root.Future<T>();
            }

            public IFutureValue<T> FutureValue<T>()
            {
                return root.FutureValue<T>();
            }

            public ICriteria GetCriteriaByAlias(string alias)
            {
                return root.GetCriteriaByAlias(alias);
            }

            public ICriteria GetCriteriaByPath(string path)
            {
                return root.GetCriteriaByPath(path);
            }

            public System.Type GetRootEntityTypeIfAvailable()
            {
                return root.GetRootEntityTypeIfAvailable();
            }

            public void List(IList results)
            {
                root.List(results);
            }

            public IList<T> List<T>()
            {
                return root.List<T>();
            }

            public string Alias
            {
                get { return subcriteriaAlias; }
            }

            public bool IsReadOnly
            {
                get { return root.IsReadOnly; }
            }

            public bool IsReadOnlyInitialized
            {
                get { return root.IsReadOnlyInitialized; }
            }

            public ICriteria SetProjection(params IProjection[] projections)
            {
                root.SetProjection(projections);
                return this;
            }

            public ICriteria Add(ICriterion criterion)
            {
                ApplyActionToShards(c => c.Add(criterion));
                return this;
            }

            public ICriteria AddOrder(Order order)
            {
                root.AddOrder(order);
                return this;
            }

            public ICriteria SetFetchMode(string associationPath, FetchMode fetchMode)
            {
                root.SetFetchMode(associationPath, fetchMode);
                return this;
            }

            public ICriteria SetLockMode(LockMode lockMode)
            {
                root.SetLockMode(lockMode);
                return this;
            }

            public ICriteria SetLockMode(string alias, LockMode lockMode)
            {
                root.SetLockMode(alias, lockMode);
                return this;
            }

            public ICriteria CreateAlias(string associationPath, string alias)
            {
                root.CreateAlias(associationPath, alias);
                return this;
            }

            public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType)
            {
                root.CreateAlias(associationPath, alias, joinType);
                return this;
            }

            public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType, ICriterion withClause)
            {
                root.CreateAlias(associationPath, alias, joinType, withClause);
                return this;
            }

            public ICriteria SetResultTransformer(IResultTransformer resultTransformer)
            {
                root.SetResultTransformer(resultTransformer);
                return this;
            }

            public ICriteria SetMaxResults(int maxResults)
            {
                root.SetMaxResults(maxResults);
                return this;
            }

            public ICriteria SetFirstResult(int firstResult)
            {
                root.SetFirstResult(firstResult);
                return this;
            }

            public ICriteria SetFetchSize(int fetchSize)
            {
                root.SetFetchSize(fetchSize);
                return this;
            }

            public ICriteria SetTimeout(int timeout)
            {
                root.SetTimeout(timeout);
                return this;
            }

            public ICriteria SetCacheable(bool cacheable)
            {
                root.SetCacheable(cacheable);
                return this;
            }

            public ICriteria SetCacheRegion(string cacheRegion)
            {
                root.SetCacheRegion(cacheRegion);
                return this;
            }

            public ICriteria SetComment(string comment)
            {
                root.SetComment(comment);
                return this;
            }

            public ICriteria SetFlushMode(FlushMode flushMode)
            {
                root.SetFlushMode(flushMode);
                return this;
            }

            public ICriteria SetCacheMode(CacheMode cacheMode)
            {
                root.SetCacheMode(cacheMode);
                return this;
            }

            public ICriteria SetReadOnly(bool readOnly)
            {
                root.SetReadOnly(readOnly);
                return this;
            }

            public IList List()
            {
                return root.List();
            }

            public object UniqueResult()
            {
                return root.UniqueResult();
            }

            public ICriteria CreateCriteria(string associationPath)
            {
                return root.CreateCriteria(associationPath);
            }

            public ICriteria CreateCriteria(string associationPath, JoinType joinType)
            {
                return root.CreateCriteria(associationPath, joinType);
            }

            public ICriteria CreateCriteria(string associationPath, string alias)
            {
                return root.CreateCriteria(associationPath, alias);
            }

            public ICriteria CreateCriteria(string associationPath, string alias, JoinType joinType)
            {
                return root.CreateCriteria(associationPath, alias, joinType);
            }

            public ICriteria CreateCriteria(string associationPath, string alias, JoinType joinType, ICriterion withClause)
            {
                return root.CreateCriteria(associationPath, alias, joinType, withClause);
            }

            public void EstablishFor(IShard shard, ICriteria parent)
            {
                ICriteria result;

                if (!this.establishedSubcriteriaByShard.TryGetValue(shard, out result))
                {
                    result = this.subcriteriaFactory(parent);
                    foreach (var action in this.establishActions)
                    {
                        action(result);
                    }
                    establishedSubcriteriaByShard[shard] = result;
                }
            }

            private void ApplyActionToShards(Action<ICriteria> action)
            {
                this.establishActions.Add(action);
                foreach (var subcriteria in this.establishedSubcriteriaByShard.Values)
                {
                    action(subcriteria);
                }
            }
        }

        #endregion
    }
}
