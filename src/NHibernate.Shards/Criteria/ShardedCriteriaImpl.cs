using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private static readonly Logger Log = new Logger(typeof(ShardedCriteriaImpl));

        #region Instance fields

        private readonly IShardedSessionImplementor session;
	    private readonly string entityName;
		private readonly Func<ISession, ICriteria> criteriaFactory;
		private readonly ExitOperationBuilder exitOperationBuilder;

		private readonly Dictionary<IShard, ICriteria> establishedCriteriaByShard = new Dictionary<IShard, ICriteria>();
		private readonly List<Action<ICriteria>> establishActions;

		private readonly Dictionary<string, ICriteria> subcriteriaByAlias;
		private readonly Dictionary<string, Subcriteria> subcriteriaByPath;

        #endregion

        #region Constructor(s)

        public ShardedCriteriaImpl(IShardedSessionImplementor session, string entityName, Func<ISession, ICriteria> criteriaFactory)
		{
			Preconditions.CheckNotNull(session);
		    Preconditions.CheckNotNull(entityName);
			Preconditions.CheckNotNull(criteriaFactory);
			this.session = session;
		    this.entityName= entityName;
			this.criteriaFactory = criteriaFactory;
			this.exitOperationBuilder = new ExitOperationBuilder();
			this.establishActions = new List<Action<ICriteria>>();
			this.subcriteriaByAlias = new Dictionary<string, ICriteria> { { CriteriaSpecification.RootAlias, this } };
			this.subcriteriaByPath = new Dictionary<string, Subcriteria>();
		}

		public ShardedCriteriaImpl(ShardedCriteriaImpl other)
		{
			Preconditions.CheckNotNull(other);
			
			this.session = other.session;
			this.criteriaFactory = other.criteriaFactory;
			this.exitOperationBuilder = new ExitOperationBuilder(other.exitOperationBuilder);
			this.establishActions = new List<Action<ICriteria>>(other.establishActions);
			this.subcriteriaByAlias = new Dictionary<string, ICriteria>(other.subcriteriaByAlias);
			this.subcriteriaByPath = new Dictionary<string, Subcriteria>(other.subcriteriaByPath);
		}

		#endregion

		#region Properties

        /// <summary>
        /// Gets arbitrary criteria implementation for a single shard.
        /// </summary>
		internal ICriteria SomeCriteria
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
				this.exitOperationBuilder.Distinct = true;
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
				this.exitOperationBuilder.Aggregation =
					c => AggregationUtil.Average(c, GetFieldSelector(0), GetFieldSelector(1));
				return this;
			}

			if (aggregationName.StartsWith("sum", StringComparison.OrdinalIgnoreCase))
			{
				ApplyActionToShards(c => c.SetProjection(projection));
				this.exitOperationBuilder.Aggregation = c => AggregationUtil.Sum(c, GetFieldSelector(0));
                return this;
			}

			if (aggregationName.StartsWith("count", StringComparison.OrdinalIgnoreCase))
			{
				ApplyActionToShards(c => c.SetProjection(projection));
				this.exitOperationBuilder.Aggregation = c => AggregationUtil.SumInt64(c, GetFieldSelector(0));
				return this;
			}

			if (aggregationName.StartsWith("min", StringComparison.OrdinalIgnoreCase))
			{
				ApplyActionToShards(c => c.SetProjection(projection));
				this.exitOperationBuilder.Aggregation = c => AggregationUtil.Min(c, GetFieldSelector(0));
				return this;
			}

			if (aggregationName.StartsWith("max", StringComparison.OrdinalIgnoreCase))
			{
				ApplyActionToShards(c => c.SetProjection(projection));
				this.exitOperationBuilder.Aggregation = c => AggregationUtil.Max(c, GetFieldSelector(0));
                return this;
			}

			var message = string.Format(
				CultureInfo.InvariantCulture,
				"Aggregate projection '{0}' is currently not supported across shards.",
				aggregationName);
			Log.Error(message);
			throw new NotSupportedException(message);
		}

		private static Func<object, object> GetFieldSelector(int fieldIndex)
		{
		    return o => GetFieldAt(o, fieldIndex);
		}

		private static object GetFieldAt(object valueOrArray, int index)
		{
			var array = valueOrArray as object[];
		    if (array == null)
		    {
		        if (index == 0) return valueOrArray;
		    }
            else if (array.Length <= index)
		    {
		        return array[index];
		    }
		    return null;
		}

		public ICriteria Add(ICriterion criterion)
		{
			ApplyActionToShards(c => c.Add(criterion));
			return this;
		}

		public ICriteria AddOrder(Order order)
		{
			this.exitOperationBuilder.Orders.Add(ToSortOrder(order));
			ApplyActionToShards(c => c.AddOrder(order));
			return this;
		}

		public void ClearOrders()
		{
			this.exitOperationBuilder.Orders.Clear();
			ApplyActionToShards(c => c.ClearOrders());
		}

		private SortOrder ToSortOrder(Order order)
		{
			var orderClause = order.ToString();
			var spaceIndex = orderClause.LastIndexOf(' ');

			var propertyPath = orderClause.Substring(0, spaceIndex);
			var orderDirection = orderClause.Substring(spaceIndex + 1);

			bool isDescending;
			if (orderClause.IndexOf("asc", spaceIndex + 1, 3, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				isDescending = false;
			}
			else if (orderClause.IndexOf("desc", spaceIndex + 1, 4, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				isDescending = true;
			}
			else
			{
				throw new ArgumentException(
					string.Format(
						CultureInfo.InvariantCulture,
						"Order '{0}' specifies invalid order direction '{1}'.",
						orderClause, orderDirection),
					"order");
			}

		    var rootClassMetadata = this.session.AnyShard.SessionFactory.GetClassMetadata(this.entityName);
		    return new SortOrder(
				o => rootClassMetadata.GetPropertyValue(o, propertyPath), 
				isDescending);
		}

		[Obsolete("Use Fetch instead")]
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
			this.exitOperationBuilder.MaxResults = maxResults;
			ApplyLimitsToShards();
			return this;
		}

		public ICriteria SetFirstResult(int firstResult)
		{
			this.exitOperationBuilder.FirstResult = firstResult;
			ApplyLimitsToShards();
			return this;
		}

		private void ApplyLimitsToShards()
		{
			if (this.exitOperationBuilder.MaxResults.HasValue)
			{
				var maxResults = this.exitOperationBuilder.MaxResults.Value
					+ this.exitOperationBuilder.FirstResult;
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
			return new List<object>(this.session.Execute(
				new ListShardOperation<object>(this), new ListExitStrategy<object>(this)));
		}

		public async Task<IList> ListAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			return new List<object>(await this.session.ExecuteAsync(
				new ListShardOperation<object>(this), new ListExitStrategy<object>(this), cancellationToken));
		}

		public IList<T> List<T>()
		{
			return new List<T>(this.session.Execute(
				new ListShardOperation<T>(this), new ListExitStrategy<T>(this)));
		}

		public async Task<IList<T>> ListAsync<T>(CancellationToken cancellationToken = new CancellationToken())
		{
			return new List<T>(await this.session.ExecuteAsync(
				new ListShardOperation<T>(this), new ListExitStrategy<T>(this), cancellationToken));
		}

		public void List(IList results)
		{
			/*
			 * We don't support shard selection for criteria queries.  If you want
			 * custom shards, create a ShardedSession with only the shards you want.
			 * We're going to concatenate all our results and then use our
			 * criteria collector to do post processing.
			 */
			var items = this.session.Execute(
				new ListShardOperation<object>(this), new ListExitStrategy<object>(this));
			foreach (var item in items)
			{
				results.Add(item);
			}
		}

		public async Task ListAsync(IList results, CancellationToken cancellationToken = new CancellationToken())
		{
			var items = await this.session.ExecuteAsync(
				new ListShardOperation<object>(this), new ListExitStrategy<object>(this), cancellationToken);
			foreach (var item in items)
			{
				results.Add(item);
			}
		}

		public object UniqueResult()
		{
			return UniqueResult<object>();
		}

		public Task<object> UniqueResultAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			return UniqueResultAsync<object>(cancellationToken);
		}

		public T UniqueResult<T>()
		{
            return this.session.Execute(new UniqueResultOperation<T>(this), new UniqueResultExitStrategy<T>(this));
		}

		public Task<T> UniqueResultAsync<T>(CancellationToken cancellationToken = new CancellationToken())
		{
			return this.session.ExecuteAsync(new UniqueResultOperation<T>(this), new UniqueResultExitStrategy<T>(this), cancellationToken);
		}

		public IFutureEnumerable<T> Future<T>()
		{
			return new FutureShardOperation<T>(this);
		}

		public IFutureValue<T> FutureValue<T>()
		{
			return new FutureValueShardOperation<T>(this);
		}

		public ExitOperation CreateExitOperation()
		{
			return this.exitOperationBuilder.BuildListOperation();
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
			return new ShardedCriteriaImpl(this);
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

        private class ListShardOperation<T> : IShardOperation<IEnumerable<T>>, IAsyncShardOperation<IEnumerable<T>>
		{
			private readonly IShardedCriteria shardedCriteria;

			public ListShardOperation(IShardedCriteria shardedCriteria)
			{
				this.shardedCriteria = shardedCriteria;
			}

			public string OperationName
			{
				get { return "List()"; }
			}

			public Func<IEnumerable<T>> Prepare(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var criteria = this.shardedCriteria.EstablishFor(shard);
				return criteria.List<T>;
			}

			public Func<CancellationToken, Task<IEnumerable<T>>> PrepareAsync(IShard shard)
			{
				var criteria = this.shardedCriteria.EstablishFor(shard);
				return async ct => await criteria.ListAsync<T>(ct);
			}
		}

		public class UniqueResultOperation<T> : IShardOperation<T>, IAsyncShardOperation<T>
		{
			private readonly IShardedCriteria shardedCriteria;

			public UniqueResultOperation(IShardedCriteria shardedCriteria)
			{
				this.shardedCriteria = shardedCriteria;
			}

			public string OperationName
			{
				get { return "uniqueResult()"; }
			}

			public Func<T> Prepare(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var criteria = shardedCriteria.EstablishFor(shard);
				return criteria.UniqueResult<T>;
			}

			public Func<CancellationToken, Task<T>> PrepareAsync(IShard shard)
			{
				var criteria = shardedCriteria.EstablishFor(shard);
				return criteria.UniqueResultAsync<T>;
			}
		}

		private class FutureShardOperation<T> : IShardOperation<IEnumerable<T>>, IAsyncShardOperation<IEnumerable<T>>, IFutureEnumerable<T>
		{
		    private IEnumerable<T> results;
			private readonly ShardedCriteriaImpl shardedCriteria;
			private readonly IDictionary<IShard, IFutureEnumerable<T>> futuresByShard;

			public FutureShardOperation(ShardedCriteriaImpl shardedCriteria)
			{
				this.shardedCriteria = shardedCriteria;
				this.futuresByShard = shardedCriteria.session.Shards
					.ToDictionary(s => s, s => shardedCriteria.EstablishFor(s).Future<T>());
			}

			public string OperationName
			{
				get { return "Future()"; }
			}

			public Func<IEnumerable<T>> Prepare(IShard shard)
			{
				return () => futuresByShard[shard];
			}

			public Func<CancellationToken, Task<IEnumerable<T>>> PrepareAsync(IShard shard)
			{
				return this.futuresByShard[shard].GetEnumerableAsync;
			}

		    public async Task<IEnumerable<T>> GetEnumerableAsync(CancellationToken cancellationToken = new CancellationToken())
		    {
		        if (this.results == null)
		        {
		            var session = this.shardedCriteria.session;
		            var exitStrategy = new ListExitStrategy<T>(this.shardedCriteria);
		            this.results = await session.ExecuteAsync(this, exitStrategy, cancellationToken).ConfigureAwait(false);
		        }
		        return this.results;
		    }

		    public IEnumerable<T> GetEnumerable()
		    {
		        if (this.results == null)
		        {
		            var session = this.shardedCriteria.session;
		            var exitStrategy = new ListExitStrategy<T>(this.shardedCriteria);
		            this.results = session.Execute(this, exitStrategy);
		        }
		        return this.results;
		    }

            public IEnumerator<T> GetEnumerator()
			{
				return GetEnumerable().GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerable().GetEnumerator();
			}
		}

		private class FutureValueShardOperation<T> : IShardOperation<T>, IAsyncShardOperation<T>, IFutureValue<T>
		{
			private readonly ShardedCriteriaImpl shardedCriteria;
			private readonly IDictionary<IShard, IFutureValue<T>> futuresByShard;

			public FutureValueShardOperation(ShardedCriteriaImpl shardedCriteria)
			{
				this.shardedCriteria = shardedCriteria;
				this.futuresByShard = shardedCriteria.session.Shards
					.ToDictionary(s => s, s => shardedCriteria.EstablishFor(s).FutureValue<T>());
			}

			public string OperationName
			{
				get { return "FutureValue()"; }
			}

			public Func<T> Prepare(IShard shard)
			{
				return () => futuresByShard[shard].Value;
			}

			public Func<CancellationToken, Task<T>> PrepareAsync(IShard shard)
			{
				return this.futuresByShard[shard].GetValueAsync;
			}

			public T Value
			{
			    get
			    {
			        var exitStrategy = new UniqueResultExitStrategy<T>(this.shardedCriteria);
                    return this.shardedCriteria.session.Execute(this, exitStrategy);
			    }
			}

			public Task<T> GetValueAsync(CancellationToken cancellationToken = new CancellationToken())
			{
			    var exitStrategy = new UniqueResultExitStrategy<T>(this.shardedCriteria);
				return this.shardedCriteria.session.ExecuteAsync(this, exitStrategy, cancellationToken);
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

			public void ClearOrders()
			{
				root.ClearOrders();
			}

			public IFutureEnumerable<T> Future<T>()
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

			public Task ListAsync(IList results, CancellationToken cancellationToken = new CancellationToken())
			{
				return this.root.ListAsync(results, cancellationToken);
			}

			public Task<IList> ListAsync(CancellationToken cancellationToken = new CancellationToken())
			{
				return this.root.ListAsync(cancellationToken);
			}

			public IList<T> List<T>()
			{
				return root.List<T>();
			}

			public Task<IList<T>> ListAsync<T>(CancellationToken cancellationToken = new CancellationToken())
			{
				return this.root.ListAsync<T>(cancellationToken);
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

			[Obsolete("Use Fetch instead")]
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

			public Task<object> UniqueResultAsync(CancellationToken cancellationToken = new CancellationToken())
			{
				return this.root.UniqueResultAsync(cancellationToken);
			}

			public T UniqueResult<T>()
			{
				return root.UniqueResult<T>();
			}

			public Task<T> UniqueResultAsync<T>(CancellationToken cancellationToken = new CancellationToken())
			{
				return this.root.UniqueResultAsync<T>(cancellationToken);
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
