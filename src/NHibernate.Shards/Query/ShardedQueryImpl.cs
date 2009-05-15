using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Shards.Strategy.Access;
using NHibernate.Shards.Util;
using NHibernate.Transform;
using NHibernate.Type;

namespace NHibernate.Shards.Query
{
    class ShardedQueryImpl : IShardedQuery
    {
        /**
 * Concrete implementation of ShardedQuery provided by Hibernate Shards. This
 * implementation introduces limits to the HQL language; mostly around
 * limits and aggregation. Its approach is simply to execute the query on
 * each shard and compile the results in a list, or if a unique result is
 * desired, the fist non-null result is returned.
 *
 * The setFoo methods are implemented using a set of classes that implement
 * the QueryEvent interface and are called SetFooEvent. These query events
 * are used to call setFoo with the appropriate arguments on each Query that
 * is executed on a shard.
 *
 * @see org.hibernate.shards.query.QueryEvent
 *
 * {@inheritDoc}
 *
 * @author Maulik Shah
 */

  private readonly QueryId queryId;
  private readonly IList<IShard> shards;
  private readonly IQueryFactory queryFactory;
  private readonly IShardAccessStrategy shardAccessStrategy;

  ///**
  // * The queryCollector is not used in ShardedQueryImpl as it would require
  // * this implementation to parse the query string and extract which exit
  // * operations would be appropriate. This member is a place holder for
  // * future development.
  // */
  //private readonly ExitOperationsQueryCollector queryCollector;

  ///**
  // * Constructor for ShardedQueryImpl
  // *
  // * @param queryId the id of the query
  // * @param shards list of shards on which this query will be executed
  // * @param queryFactory factory that knows how to create the actual query we'll execute
  // * @param shardAccessStrategy the shard strategy for this query
  // */
  //public ShardedQueryImpl(QueryId queryId,
  //                        List<IShard> shards,
  //                        IQueryFactory queryFactory,
  //                        IShardAccessStrategy shardAccessStrategy) {
  //  this.queryId = queryId;
  //  this.shards = shards;
  //  this.queryFactory = queryFactory;
  //  this.shardAccessStrategy = shardAccessStrategy;
  //  this.queryCollector = new ExitOperationsQueryCollector();

  //  Preconditions.CheckState(!shards.isEmpty());
  //  foreach (IShard shard in shards) {
  //    Preconditions.CheckNotNull(shard);
  //  }
  //}

  //public QueryId GetQueryId() {
  //  return queryId;
  //}

  //public IQueryFactory GetQueryFactory() {
  //  return queryFactory;
  //}

  //private IQuery GetSomeQuery() {
  //  foreach (IShard shard in shards) {
  //    IQuery query = shard.GetQueryById(queryId);
  //    if (query != null) {
  //      return query;
  //    }
  //  }
  //  return null;
  //}

  //private IQuery GetOrEstablishSomeQuery() {
  //  IQuery query = GetSomeQuery();
  //  if (query == null) {
  //    IShard shard = shards.get(0);
  //    query = shard.EstablishQuery(this);
  //  }
  //  return query;
  //}

  //public string GetQueryString() {
  //  return GetOrEstablishSomeQuery().getQueryString();
  //}

  //public Type[] GetReturnTypes() {
  //  return GetOrEstablishSomeQuery().GetReturnTypes();
  //}

       
  //      public string[] GetReturnAliases(){
  //  return GetOrEstablishSomeQuery().getReturnAliases();
  //}

  //public string[] GetNamedParameters(){
  //  return GetOrEstablishSomeQuery().getNamedParameters();
  //}

  ///**
  // * This method currently wraps list().
  // *
  // * {@inheritDoc}
  // *
  // * @return an iterator over the results of the query
  // * @throws HibernateException
  // */
  //public IList Iterate(){
  //  /**
  //   * TODO(maulik) Hibernate in Action says these two methods are equivalent
  //   * in what the content that they return but are implemented differently.
  //   * We should figure out the difference and implement correctly.
  //   */
  //  return list().iterator();
  //}

  ///**
  // * Scrolling is unsupported. Current implementation throws an
  // * UnsupportedOperationException. A dumb implementation of scroll might be
  // * possible; however it would provide no performance benefit. An intelligent
  // * implementation would require re-querying shards frequently and a
  // * deterministic way to complie results.
  // */
  //public ScrollableResults scroll() {
  //  throw new UnsupportedOperationException();
  //}

  ///**
  // * Scrolling is unsupported. Current implementation throws an
  // * UnsupportedOperationException. A dumb implementation of scroll might be
  // * possible; however it would provide no performance benefit. An intelligent
  // * implementation would require re-querying shards frequently and a
  // * deterministic way to complie results.
  // */
  //public ScrollableResults scroll(ScrollMode scrollMode) throws HibernateException {
  //  throw new UnsupportedOperationException();
  //}

  ///**
  // * The implementation executes the query on each shard and concatenates the
  // * results.
  // *
  // * {@inheritDoc}
  // *
  // * @return a list containing the concatenated results of executing the
  // * query on all shards
  // * @throws HibernateException
  // */
  //public IList list() {
  //  IShardOperation<List<Object>> shardOp = new IShardOperation<List<Object>>() {
  //    public List<Object> execute(IShard shard) {
  //      shard.establishQuery(ShardedQueryImpl.this);
  //      return shard.list(queryId);
  //    }

  //    public String getOperationName() {
  //      return "list()";
  //    }
  //  };
  //  /**
  //   * We don't support shard selection for HQL queries.  If you want
  //   * custom shards, create a ShardedSession with only the shards you want.
  //   */
  //  return
  //    shardAccessStrategy.apply(
  //            shards,
  //            shardOp,
  //            new ConcatenateListsExitStrategy(),
  //            queryCollector);

  //}

  ///**
  // * The implementation executes the query on each shard and returns the first
  // * non-null result.
  // *
  // * {@inheritDoc}
  // *
  // * @return the first non-null result, or null if no non-null result found
  // * @throws HibernateException
  // */
  //public Object uniqueResult() throws HibernateException {
  //  ShardOperation<Object> shardOp = new ShardOperation<Object>() {
  //    public Object execute(Shard shard) {
  //      shard.establishQuery(ShardedQueryImpl.this);
  //      return shard.uniqueResult(queryId);
  //    }

  //    public String getOperationName() {
  //      return "uniqueResult()";
  //    }
  //  };
  // /**
  //  * We don't support shard selection for HQL queries.  If you want
  //  * custom shards, create a ShardedSession with only the shards you want.
  //  */
  //  return
  //    shardAccessStrategy.apply(
  //            shards,
  //            shardOp,
  //            new FirstNonNullResultExitStrategy<Object>(),
  //            queryCollector);
  //}

  ///**
  // * ExecuteUpdate is not supported and throws an
  // * UnsupportedOperationException.
  // *
  // * @throws HibernateException
  // */
  //public int executeUpdate() throws HibernateException {
  //  throw new UnsupportedOperationException();
  //}

  //public Query setMaxResults(int maxResults) {
  //  queryCollector.setMaxResults(maxResults);
  //  return this;
  //}

  //public Query setFirstResult(int firstResult) {
  //  queryCollector.setFirstResult(firstResult);
  //  return this;
  //}

  //public Query setReadOnly(boolean readOnly) {
  //  QueryEvent event = new SetReadOnlyEvent(readOnly);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setReadOnly(readOnly);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setCacheable(boolean cacheable) {
  //  QueryEvent event = new SetCacheableEvent(cacheable);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setCacheable(cacheable);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setCacheRegion(String cacheRegion) {
  //  QueryEvent event = new SetCacheRegionEvent(cacheRegion);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setCacheRegion(cacheRegion);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setTimeout(int timeout) {
  //  QueryEvent event = new SetTimeoutEvent(timeout);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setTimeout(timeout);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setFetchSize(int fetchSize) {
  //  QueryEvent event = new SetFetchSizeEvent(fetchSize);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setFetchSize(fetchSize);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setLockMode(String alias, LockMode lockMode) {
  //  QueryEvent event = new SetLockModeEvent(alias, lockMode);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setLockMode(alias, lockMode);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setComment(String comment) {
  //  QueryEvent event = new SetCommentEvent(comment);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setComment(comment);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setFlushMode(FlushMode flushMode) {
  //  QueryEvent event = new SetFlushModeEvent(flushMode);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setFlushMode(flushMode);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setCacheMode(CacheMode cacheMode) {
  //  QueryEvent event = new SetCacheModeEvent(cacheMode);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setCacheMode(cacheMode);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setParameter(int position, Object val, Type type) {
  //  QueryEvent event = new SetParameterEvent(position, val, type);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setParameter(position, val, type);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setParameter(String name, Object val, Type type) {
  //  QueryEvent event = new SetParameterEvent(name, val, type);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setParameter(name, val, type);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setParameter(int position, Object val) throws HibernateException {
  //  QueryEvent event = new SetParameterEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setParameter(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setParameter(String name, Object val) throws HibernateException {
  //  QueryEvent event = new SetParameterEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setParameter(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setParameters(Object[] values, Type[] types) throws HibernateException {
  //  QueryEvent event = new SetParametersEvent(values, types);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setParameters(values, types);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setParameterList(String name, Collection vals, Type type) throws HibernateException {
  //  QueryEvent event = new SetParameterListEvent(name, vals, type);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setParameterList(name, vals, type);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setParameterList(String name, Collection vals) throws HibernateException {
  //  QueryEvent event = new SetParameterListEvent(name, vals);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setParameterList(name, vals);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setParameterList(String name, Object[] vals, Type type) throws HibernateException {
  //  QueryEvent event = new SetParameterListEvent(name, vals, type);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setParameterList(name, vals, type);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setParameterList(String name, Object[] vals) throws HibernateException {
  //  QueryEvent event = new SetParameterListEvent(name, vals);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setParameterList(name, vals);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setProperties(Object bean) throws HibernateException {
  //  QueryEvent event = new SetPropertiesEvent(bean);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setProperties(bean);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setString(int position, String val) {
  //  QueryEvent event = new SetStringEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setString(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setCharacter(int position, char val) {
  //  QueryEvent event = new SetCharacterEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setCharacter(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setBoolean(int position, boolean val) {
  //  QueryEvent event = new SetBooleanEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setBoolean(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setByte(int position, byte val) {
  //  QueryEvent event = new SetByteEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setByte(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setShort(int position, short val) {
  //  QueryEvent event = new SetShortEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setShort(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setInteger(int position, int val) {
  //  QueryEvent event = new SetIntegerEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setInteger(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setLong(int position, long val) {
  //  QueryEvent event = new SetLongEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setLong(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setFloat(int position, float val) {
  //  QueryEvent event = new SetFloatEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setFloat(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setDouble(int position, double val) {
  //  QueryEvent event = new SetDoubleEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setDouble(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setBinary(int position, byte[] val) {
  //  QueryEvent event = new SetBinaryEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setBinary(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setText(int position, String val) {
  //  QueryEvent event = new SetTextEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setText(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setSerializable(int position, Serializable val) {
  //  QueryEvent event = new SetSerializableEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setSerializable(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setLocale(int position, Locale locale) {
  //  QueryEvent event = new SetLocaleEvent(position, locale);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setLocale(position, locale);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setBigDecimal(int position, BigDecimal number) {
  //  QueryEvent event = new SetBigDecimalEvent(position, number);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setBigDecimal(position, number);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setBigInteger(int position, BigInteger number) {
  //  QueryEvent event = new SetBigIntegerEvent(position, number);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setBigInteger(position, number);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setDate(int position, Date date) {
  //  QueryEvent event = new SetDateEvent(position, date);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setDate(position, date);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setTime(int position, Date date) {
  //  QueryEvent event = new SetTimeEvent(position, date);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setTime(position, date);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setTimestamp(int position, Date date) {
  //  QueryEvent event = new SetTimestampEvent(position, date);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setTimestamp(position, date);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setCalendar(int position, Calendar calendar) {
  //  QueryEvent event = new SetCalendarEvent(position, calendar);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setCalendar(position, calendar);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setCalendarDate(int position, Calendar calendar) {
  //  QueryEvent event = new SetCalendarDateEvent(position, calendar);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setCalendarDate(position, calendar);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setString(String name, String val) {
  //  QueryEvent event = new SetStringEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setString(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setCharacter(String name, char val) {
  //  QueryEvent event = new SetCharacterEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setCharacter(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setBoolean(String name, boolean val) {
  //  QueryEvent event = new SetBooleanEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setBoolean(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setByte(String name, byte val) {
  //  QueryEvent event = new SetByteEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setByte(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setShort(String name, short val) {
  //  QueryEvent event = new SetShortEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setShort(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setInteger(String name, int val) {
  //  QueryEvent event = new SetIntegerEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setInteger(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setLong(String name, long val) {
  //  QueryEvent event = new SetLongEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setLong(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setFloat(String name, float val) {
  //  QueryEvent event = new SetFloatEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setFloat(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setDouble(String name, double val) {
  //  QueryEvent event = new SetDoubleEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setDouble(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setBinary(String name, byte[] val) {
  //  QueryEvent event = new SetBinaryEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setBinary(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setText(String name, String val) {
  //  QueryEvent event = new SetTextEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setText(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setSerializable(String name, Serializable val) {
  //  QueryEvent event = new SetSerializableEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setSerializable(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setLocale(String name, Locale locale) {
  //  QueryEvent event = new SetLocaleEvent(name, locale);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setLocale(name, locale);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setBigDecimal(String name, BigDecimal number) {
  //  QueryEvent event = new SetBigDecimalEvent(name, number);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setBigDecimal(name, number);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setBigInteger(String name, BigInteger number) {
  //  QueryEvent event = new SetBigIntegerEvent(name, number);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setBigInteger(name, number);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setDate(String name, Date date) {
  //  QueryEvent event = new SetDateEvent(name, date);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setDate(name, date);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setTime(String name, Date date) {
  //  QueryEvent event = new SetTimeEvent(name, date);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setTime(name, date);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setTimestamp(String name, Date date) {
  //  QueryEvent event = new SetTimestampEvent(name, date);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setTimestamp(name, date);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setCalendar(String name, Calendar calendar) {
  //  QueryEvent event = new SetCalendarEvent(name, calendar);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setCalendar(name, calendar);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setCalendarDate(String name, Calendar calendar) {
  //  QueryEvent event = new SetCalendarDateEvent(name, calendar);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setCalendarDate(name, calendar);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setEntity(int position, Object val) {
  //  QueryEvent event = new SetEntityEvent(position, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setEntity(position, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setEntity(String name, Object val) {
  //  QueryEvent event = new SetEntityEvent(name, val);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setEntity(name, val);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setResultTransformer(ResultTransformer transformer) {
  //  QueryEvent event = new SetResultTransformerEvent(transformer);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setResultTransformer(transformer);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}

  //public Query setProperties(Map map) throws HibernateException {
  //  QueryEvent event = new SetPropertiesEvent(map);
  //  for (Shard shard : shards) {
  //    if (shard.getQueryById(queryId) != null) {
  //      shard.getQueryById(queryId).setProperties(map);
  //    } else {
  //      shard.addQueryEvent(queryId, event);
  //    }
  //  }
  //  return this;
  //}
        public IEnumerable Enumerable()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Enumerable<T>()
        {
            throw new NotImplementedException();
        }

        public IList List()
        {
            throw new NotImplementedException();
        }

        public void List(IList results)
        {
            throw new NotImplementedException();
        }

        public IList<T> List<T>()
        {
            throw new NotImplementedException();
        }

        public object UniqueResult()
        {
            throw new NotImplementedException();
        }

        public T UniqueResult<T>()
        {
            throw new NotImplementedException();
        }

        public int ExecuteUpdate()
        {
            throw new NotImplementedException();
        }

        public IQuery SetMaxResults(int maxResults)
        {
            throw new NotImplementedException();
        }

        public IQuery SetFirstResult(int firstResult)
        {
            throw new NotImplementedException();
        }

        public IQuery SetReadOnly(bool readOnly)
        {
            throw new NotImplementedException();
        }

        public IQuery SetCacheable(bool cacheable)
        {
            throw new NotImplementedException();
        }

        public IQuery SetCacheRegion(string cacheRegion)
        {
            throw new NotImplementedException();
        }

        public IQuery SetTimeout(int timeout)
        {
            throw new NotImplementedException();
        }

        public IQuery SetFetchSize(int fetchSize)
        {
            throw new NotImplementedException();
        }

        public IQuery SetLockMode(string alias, LockMode lockMode)
        {
            throw new NotImplementedException();
        }

        public IQuery SetComment(string comment)
        {
            throw new NotImplementedException();
        }

        public IQuery SetFlushMode(FlushMode flushMode)
        {
            throw new NotImplementedException();
        }

        public IQuery SetCacheMode(CacheMode cacheMode)
        {
            throw new NotImplementedException();
        }

        public IQuery SetParameter(int position, object val, IType type)
        {
            throw new NotImplementedException();
        }

        public IQuery SetParameter(string name, object val, IType type)
        {
            throw new NotImplementedException();
        }

        public IQuery SetParameter<T>(int position, T val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetParameter<T>(string name, T val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetParameter(int position, object val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetParameter(string name, object val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetParameterList(string name, ICollection vals, IType type)
        {
            throw new NotImplementedException();
        }

        public IQuery SetParameterList(string name, ICollection vals)
        {
            throw new NotImplementedException();
        }

        public IQuery SetParameterList(string name, object[] vals, IType type)
        {
            throw new NotImplementedException();
        }

        public IQuery SetParameterList(string name, object[] vals)
        {
            throw new NotImplementedException();
        }

        public IQuery SetProperties(object obj)
        {
            throw new NotImplementedException();
        }

        public IQuery SetAnsiString(int position, string val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetAnsiString(string name, string val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetBinary(int position, byte[] val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetBinary(string name, byte[] val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetBoolean(int position, bool val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetBoolean(string name, bool val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetByte(int position, byte val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetByte(string name, byte val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetCharacter(int position, char val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetCharacter(string name, char val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetDateTime(int position, DateTime val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetDateTime(string name, DateTime val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetDecimal(int position, decimal val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetDecimal(string name, decimal val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetDouble(int position, double val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetDouble(string name, double val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetEnum(int position, Enum val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetEnum(string name, Enum val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetInt16(int position, short val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetInt16(string name, short val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetInt32(int position, int val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetInt32(string name, int val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetInt64(int position, long val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetInt64(string name, long val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetSingle(int position, float val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetSingle(string name, float val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetString(int position, string val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetString(string name, string val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetTime(int position, DateTime val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetTime(string name, DateTime val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetTimestamp(int position, DateTime val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetTimestamp(string name, DateTime val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetGuid(int position, Guid val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetGuid(string name, Guid val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetEntity(int position, object val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetEntity(string name, object val)
        {
            throw new NotImplementedException();
        }

        public IQuery SetResultTransformer(IResultTransformer resultTransformer)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Future<T>()
        {
            throw new NotImplementedException();
        }

        public IFutureValue<T> FutureValue<T>()
        {
            throw new NotImplementedException();
        }

        public string QueryString
        {
            get { throw new NotImplementedException(); }
        }

        public IType[] ReturnTypes
        {
            get { throw new NotImplementedException(); }
        }

        public string[] ReturnAliases
        {
            get { throw new NotImplementedException(); }
        }

        public string[] NamedParameters
        {
            get { throw new NotImplementedException(); }
        }

        public QueryId QueryId
        {
            get { throw new NotImplementedException(); }
        }

        public IQueryFactory QueryFactory
        {
            get { throw new NotImplementedException(); }
        }
    }
}
