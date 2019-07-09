using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Engine.Query;
using NHibernate.Hql;
using NHibernate.Impl;
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
		private readonly IQueryExpressionPlan unshardedQueryExpressionPlan;
		private ShardedQueryExpression queryExpression;

		private readonly IDictionary<IShard, IQuery> establishedQueriesByShard = new Dictionary<IShard, IQuery>();
		private Action<IQuery> establishActions;

		private readonly ExitOperationBuilder exitOperationBuilder = new ExitOperationBuilder();

		/// <summary>
		/// Creates new <see cref="ShardedQueryImpl"/> instance.
		/// </summary>
		/// <param name="session">The Sharded session on which this query is to be executed.</param>
		/// <param name="hql">An HQL query string.</param>
		public static ShardedQueryImpl CreateQuery(IShardedSessionImplementor session, string hql)
		{
			var anySessionImplementor = (AbstractSessionImpl)session.AnyShard.EstablishSession();
			var unshardedQueryExpression = new StringQueryExpression(hql);
			var unshardedQueryPlan = anySessionImplementor.Factory.QueryPlanCache.GetHQLQueryPlan(
				unshardedQueryExpression, false, anySessionImplementor.EnabledFilters);
			return new ShardedQueryImpl(session, unshardedQueryPlan);
		}

		public static ShardedQueryImpl GetNamedQuery(IShardedSessionImplementor session, string queryName)
		{
			var anySession = session.AnyShard.EstablishSession();
			var query = anySession.GetNamedQuery(queryName);
			return query is ISQLQuery
				? new ShardedQueryImpl(session, s => s.GetNamedQuery(queryName))
				: CreateQuery(session, query.QueryString);
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

		/// <summary>
		/// Creates new <see cref="ShardedQueryImpl"/> instance.
		/// </summary>
		/// <param name="session">The Sharded session on which this query is to be executed.</param>
		/// <param name="unshardedQueryExpressionPlan">A shard-local <see cref="IQueryExpressionPlan"/> that represents 
		/// a parsed HQL string or the HQL equivalent of a Linq expression.</param>
		public ShardedQueryImpl(IShardedSessionImplementor session, IQueryExpressionPlan unshardedQueryExpressionPlan)
		{
			Preconditions.CheckNotNull(session);
			Preconditions.CheckNotNull(unshardedQueryExpressionPlan);
			this.session = session;
			this.unshardedQueryExpressionPlan = unshardedQueryExpressionPlan;
			this.queryFactory = s => ApplyLimits(s.GetSessionImplementation().CreateQuery(this.QueryExpression));
		}

		private IQuery ApplyLimits(IQuery query)
		{
			query.SetFirstResult(0);
			if (this.exitOperationBuilder.MaxResults != null)
			{
				query.SetMaxResults(this.exitOperationBuilder.FirstResult + this.exitOperationBuilder.MaxResults.Value);
			}
			return query;
		}

		public IShardedSessionImplementor Session
		{
			get { return this.session; }
		}

		public ShardedQueryExpression QueryExpression
		{
			get
			{
				if (this.queryExpression == null && this.unshardedQueryExpressionPlan != null)
				{
					this.queryExpression = new ShardedQueryExpression(this.unshardedQueryExpressionPlan, this.exitOperationBuilder);
				}
				return this.queryExpression;
			}
		}

		public bool IsReadOnly
		{
			get { return this.SomeQuery.IsReadOnly; }
		}

		public IEnumerable Enumerable()
		{
			return Enumerable<object>();
		}

		public async Task<IEnumerable> EnumerableAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			return await EnumerableAsync<object>(cancellationToken);
		}

		public IEnumerable<T> Enumerable<T>()
		{
			return this.session.Execute(new ListShardOperation<T>(this), new ListExitStrategy<T>(this));
		}

		public async Task<IEnumerable<T>> EnumerableAsync<T>(CancellationToken cancellationToken = new CancellationToken())
		{
			return await this.session.ExecuteAsync(new ListShardOperation<T>(this), new ListExitStrategy<T>(this), cancellationToken);
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
		public virtual IList List()
		{
			IList results;

			var resultType = this.queryExpression?.Type;
			if (this.exitOperationBuilder.Aggregation == null)
			{
				results = CreateListResult(resultType);
				List(results);
			}
			else
			{
				var aggregationResults = List<object>();
				if (resultType == null)
				{
					results = (IList) aggregationResults;
				}
				else
				{
					var aggregationResultType = aggregationResults[0].GetType();
					results = CreateListResult(resultType);
					results.Add(resultType.IsAssignableFrom(aggregationResultType)
						? aggregationResults[0]
						: Convert.ChangeType(aggregationResults[0], resultType, CultureInfo.InvariantCulture));
				}
			}

			return results;
		}

		public async Task<IList> ListAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			IList results;

			var resultType = this.queryExpression?.Type;
			if (this.exitOperationBuilder.Aggregation == null)
			{
				results = CreateListResult(resultType);
				await ListAsync(results, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				var aggregationResults = await ListAsync<object>(cancellationToken).ConfigureAwait(false);
				if (resultType == null)
				{
					results = (IList)aggregationResults;
				}
				else
				{
					var aggregationResultType = aggregationResults[0].GetType();
					results = CreateListResult(resultType);
					results.Add(resultType.IsAssignableFrom(aggregationResultType)
						? aggregationResults[0]
						: Convert.ChangeType(aggregationResults[0], resultType, CultureInfo.InvariantCulture));
				}
			}

			return results;
		}

		private IList CreateListResult(System.Type elementType = null)
		{
			return elementType != null && elementType != typeof(object)
				? (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))
				: new List<object>();
		}


		public void List(IList results)
		{
			foreach (var item in Enumerable())
			{
				results.Add(item);
			}
		}

		public async Task ListAsync(IList results, CancellationToken cancellationToken = new CancellationToken())
		{
			foreach (var item in await EnumerableAsync(cancellationToken))
			{
				results.Add(item);
			}
		}

		public IList<T> List<T>()
		{
			return Enumerable<T>().ToList();
		}

		public async Task<IList<T>> ListAsync<T>(CancellationToken cancellationToken = new CancellationToken())
		{
			return (await EnumerableAsync<T>(cancellationToken)).ToList();
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

		public Task<object> UniqueResultAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			return UniqueResultAsync<object>(cancellationToken);
		}

		public T UniqueResult<T>()
		{
			return this.session.Execute(new UniqueResultShardOperation<T>(this), new UniqueResultExitStrategy<T>(this));
		}

		public Task<T> UniqueResultAsync<T>(CancellationToken cancellationToken = new CancellationToken())
		{
			return this.session.ExecuteAsync(new UniqueResultShardOperation<T>(this), new UniqueResultExitStrategy<T>(this), cancellationToken);
		}

		public IFutureEnumerable<T> Future<T>()
		{
			return new FutureShardOperation<T>(this);
		}

		public IFutureValue<T> FutureValue<T>()
		{
			return new FutureValueShardOperation<T>(this);
		}

		public int ExecuteUpdate()
		{
			return this.session.Execute(new ExecuteUpdateShardOperation(this), new ExecuteUpdateExitStrategy());
		}

		public Task<int> ExecuteUpdateAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			return this.session.ExecuteAsync(new ExecuteUpdateShardOperation(this), new ExecuteUpdateExitStrategy(), cancellationToken);
		}

		public IQuery SetMaxResults(int maxResults)
		{
			this.exitOperationBuilder.MaxResults = maxResults;
			return this;
		}

		public IQuery SetFirstResult(int firstResult)
		{
			this.exitOperationBuilder.FirstResult = firstResult;
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

		public IQuery SetDateTimeNoMs(int position, DateTime val)
		{
			ApplyActionToShards(q => q.SetDateTimeNoMs(position, val));
			return this;
		}

		public IQuery SetDateTimeNoMs(string name, DateTime val)
		{
			ApplyActionToShards(q => q.SetDateTimeNoMs(name, val));
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

		[Obsolete("Use method 'SetDateTime'")]
		public IQuery SetTimestamp(int position, DateTime val)
		{
			ApplyActionToShards(q => q.SetTimestamp(position, val));
			return this;
		}

		[Obsolete("Use method 'SetDateTime'")]
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

		[Obsolete("Use method 'SetDateTime', which will use DateTime2 on dialects that support it.")]
		public IQuery SetDateTime2(string name, DateTime val)
		{
			ApplyActionToShards(q => q.SetDateTime2(name, val));
			return this;
		}

		[Obsolete("Use method 'SetDateTime', which will use DateTime2 on dialects that support it.")]
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
			this.establishActions += action;
			foreach (var query in this.establishedQueriesByShard.Values)
			{
				action(query);
			}
		}

		internal IQuery SomeQuery
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
			if (!establishedQueriesByShard.TryGetValue(shard, out var result))
			{
				result = this.queryFactory(shard.EstablishSession());
				this.establishActions?.Invoke(result);
				establishedQueriesByShard.Add(shard, result);
			}
			return result;
		}

		public ExitOperation CreateExitOperation()
		{
			return this.exitOperationBuilder.BuildListOperation();
		}

		private class UniqueResultShardOperation<T> : IShardOperation<T>, IAsyncShardOperation<T>
		{
			private readonly IShardedQuery shardedQuery;

			public UniqueResultShardOperation(IShardedQuery shardedQuery)
			{
				this.shardedQuery = shardedQuery;
			}

			public Func<T> Prepare(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var query = this.shardedQuery.EstablishFor(shard);
				return query.UniqueResult<T>;
			}

			public Func<CancellationToken, Task<T>> PrepareAsync(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var query = this.shardedQuery.EstablishFor(shard);
				return query.UniqueResultAsync<T>;
			}

			public string OperationName
			{
				get { return "UniqueResult()"; }
			}
		}

		private class ListShardOperation<T> : IShardOperation<IEnumerable<T>>, IAsyncShardOperation<IEnumerable<T>>
		{
			private readonly IShardedQuery shardedQuery;

			public ListShardOperation(IShardedQuery shardedQuery)
			{
				this.shardedQuery = shardedQuery;
			}

			public Func<IEnumerable<T>> Prepare(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var query = this.shardedQuery.EstablishFor(shard);
				return query.List<T>;
			}

			public Func<CancellationToken, Task<IEnumerable<T>>> PrepareAsync(IShard shard)
			{
				var query = this.shardedQuery.EstablishFor(shard);
				return async ct => await query.ListAsync<T>(ct);
			}

			public string OperationName
			{
				get { return "List()"; }
			}
		}

		private class FutureShardOperation<T> : IShardOperation<IEnumerable<T>>, IAsyncShardOperation<IEnumerable<T>>, IFutureEnumerable<T>
		{
			private IEnumerable<T> results;
			private readonly ShardedQueryImpl shardedQuery;
			private readonly IDictionary<IShard, IFutureEnumerable<T>> futuresByShard;

			public FutureShardOperation(ShardedQueryImpl shardedQuery)
			{
				this.shardedQuery = shardedQuery;
				this.futuresByShard = shardedQuery.session.Shards
					.ToDictionary(s => s, s => shardedQuery.EstablishFor(s).Future<T>());
			}

			public string OperationName
			{
				get { return "Future()"; }
			}

			public Func<IEnumerable<T>> Prepare(IShard shard)
			{
				return () => this.futuresByShard[shard];
			}

			public Func<CancellationToken, Task<IEnumerable<T>>> PrepareAsync(IShard shard)
			{
				return this.futuresByShard[shard].GetEnumerableAsync;
			}

			public async Task<IEnumerable<T>> GetEnumerableAsync(CancellationToken cancellationToken = new CancellationToken())
			{
				if (this.results == null)
				{
					var exitStrategy = new ListExitStrategy<T>(this.shardedQuery);
					this.results = await this.shardedQuery.session.ExecuteAsync(this, exitStrategy, cancellationToken).ConfigureAwait(false);
				}
				return this.results;
			}

			public IEnumerable<T> GetEnumerable()
			{
				if (this.results == null)
				{
					var exitStrategy = new ListExitStrategy<T>(this.shardedQuery);
					this.results = this.shardedQuery.session.Execute(this, exitStrategy);
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
			private readonly ShardedQueryImpl shardedQuery;
			private readonly IDictionary<IShard, IFutureValue<T>> futuresByShard;

			public FutureValueShardOperation(ShardedQueryImpl shardedQuery)
			{
				this.shardedQuery = shardedQuery;
				this.futuresByShard = shardedQuery.session.Shards
					.ToDictionary(s => s, s => shardedQuery.EstablishFor(s).FutureValue<T>());
			}

			public string OperationName
			{
				get { return "FutureValue()"; }
			}

			public Func<T> Prepare(IShard shard)
			{
				return () => this.futuresByShard[shard].Value;
			}

			public Func<CancellationToken, Task<T>> PrepareAsync(IShard shard)
			{
				return this.futuresByShard[shard].GetValueAsync;
			}

			public T Value
			{
				get
				{
					var exitStrategy = new UniqueResultExitStrategy<T>(this.shardedQuery);
					return this.shardedQuery.session.Execute(this, exitStrategy);
				}
			}

			public Task<T> GetValueAsync(CancellationToken cancellationToken = new CancellationToken())
			{
				var exitStrategy = new UniqueResultExitStrategy<T>(this.shardedQuery);
				return this.shardedQuery.session.ExecuteAsync(this, exitStrategy, cancellationToken);
			}
		}

		private class ExecuteUpdateShardOperation : IShardOperation<int>, IAsyncShardOperation<int>
		{
			private readonly IShardedQuery shardedQuery;

			public ExecuteUpdateShardOperation(IShardedQuery shardedQuery)
			{
				this.shardedQuery = shardedQuery;
			}

			public Func<int> Prepare(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var query = this.shardedQuery.EstablishFor(shard);
				return query.ExecuteUpdate;
			}

			public Func<CancellationToken, Task<int>> PrepareAsync(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var query = this.shardedQuery.EstablishFor(shard);
				return query.ExecuteUpdateAsync;
			}

			public string OperationName
			{
				get { return "ExecuteUpdate()"; }
			}
		}
	}
}