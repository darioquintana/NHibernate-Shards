using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Shards.Strategy.Access;
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
		private readonly ExitOperationsQueryCollector queryCollector;

		///**
		// * Constructor for ShardedQueryImpl
		// *
		// * @param queryId the id of the query
		// * @param shards list of shards on which this query will be executed
		// * @param queryFactory factory that knows how to create the actual query we'll execute
		// * @param shardAccessStrategy the shard strategy for this query
		// */
		public ShardedQueryImpl(QueryId queryId,
		                        List<IShard> shards,
		                        IQueryFactory queryFactory,
		                        IShardAccessStrategy shardAccessStrategy)
		{
			this.queryId = queryId;
			this.shards = shards;
			this.queryFactory = queryFactory;
			this.shardAccessStrategy = shardAccessStrategy;
            this.queryCollector = new ExitOperationsQueryCollector();

			Preconditions.CheckState(!(shards.Count == 0));
			foreach (IShard shard in shards)
			{
				Preconditions.CheckNotNull(shard);
			}
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
			return List();
		}

		public IEnumerable<T> Enumerable<T>()
		{
			return List<T>();
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
			IShardOperation<IList> shardOp = new ListShardOperation<IList>(queryId, this);
			IExitStrategy<IList> exitStrategy = new ConcatenateListsExitStrategy();
			/**
			 * We don't support shard selection for HQL queries.  If you want
			 * custom shards, create a ShardedSession with only the shards you want.
			 */
			return shardAccessStrategy.Apply(shards, shardOp, exitStrategy, queryCollector);
		}

		public void List(IList results)
		{
			throw new NotImplementedException();
		}

		public IList<T> List<T>()
		{
			throw new NotImplementedException();
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
            IShardOperation<object> shardOp = new UniqueResultShardOperation<object>(queryId, this);
			/**
			 * We don't support shard selection for HQL queries.  If you want
			 * custom shards, create a ShardedSession with only the shards you want.
			 */
			return shardAccessStrategy.Apply(shards, shardOp, new FirstNonNullResultExitStrategy<object>(), queryCollector);

		}

		public T UniqueResult<T>()
		{
			IShardOperation<T> shardOp = new ListShardOperation<T>(queryId, this);
			return shardAccessStrategy.Apply(shards, shardOp, new FirstNonNullResultExitStrategy<T>(), queryCollector);
		}

		/**
		 * ExecuteUpdate is not supported and throws an
		 * UnsupportedOperationException.
		 *
		 * @throws HibernateException
		 */
		public int ExecuteUpdate()
		{
			throw new NotSupportedException();
		}

		public IQuery SetMaxResults(int maxResults)
		{
			queryCollector.SetMaxResults(maxResults);
			return this;
		}

		public IQuery SetFirstResult(int firstResult)
		{
			queryCollector.SetFirstResult(firstResult);
			return this;
		}

		public IQuery SetReadOnly(bool readOnly)
		{
			IQueryEvent queryEvent = new SetReadOnlyEvent(readOnly);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetReadOnly(readOnly);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetCacheable(bool cacheable)
		{
			IQueryEvent queryEvent = new SetCacheableEvent(cacheable);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetCacheable(cacheable);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetCacheRegion(string cacheRegion)
		{
			IQueryEvent queryEvent = new SetCacheRegionEvent(cacheRegion);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetCacheRegion(cacheRegion);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetTimeout(int timeout)
		{
			IQueryEvent queryEvent = new SetTimeoutEvent(timeout);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetTimeout(timeout);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetFetchSize(int fetchSize)
		{
			IQueryEvent queryEvent = new SetFetchSizeEvent(fetchSize);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetFetchSize(fetchSize);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetLockMode(string alias, LockMode lockMode)
		{
			IQueryEvent queryEvent = new SetLockModeEvent(alias, lockMode);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetLockMode(alias, lockMode);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetComment(string comment)
		{
			IQueryEvent queryEvent = new SetCommentEvent(comment);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetComment(comment);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetFlushMode(FlushMode flushMode)
		{
			IQueryEvent queryEvent = new SetFlushModeEvent(flushMode);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetFlushMode(flushMode);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetCacheMode(CacheMode cacheMode)
		{
			IQueryEvent queryEvent = new SetCacheModeEvent(cacheMode);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetCacheMode(cacheMode);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetParameter(int position, object val, IType type)
		{
			IQueryEvent queryEvent = new SetParameterEvent(position, val, type);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetParameter(position, val, type);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetParameter(string name, object val, IType type)
		{
			IQueryEvent queryEvent = new SetParameterEvent(name, val, type);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetParameter(name, val, type);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetParameter<T>(int position, T val)
		{
			IQueryEvent queryEvent = new SetParameterEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetParameter(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetParameter<T>(string name, T val)
		{
			IQueryEvent queryEvent = new SetParameterEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetParameter(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetParameter(int position, object val)
		{
			IQueryEvent queryEvent = new SetParameterEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetParameter(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetParameter(string name, object val)
		{
			IQueryEvent queryEvent = new SetParameterEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetParameter(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetParameterList(string name, ICollection vals, IType type)
		{
			IQueryEvent queryEvent = new SetParameterListEvent(name, vals, type);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetParameterList(name, vals, type);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetParameterList(string name, ICollection vals)
		{
			IQueryEvent queryEvent = new SetParameterListEvent(name, vals);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetParameterList(name, vals);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetParameterList(string name, object[] vals, IType type)
		{
			IQueryEvent queryEvent = new SetParameterListEvent(name, vals, type);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetParameterList(name, vals, type);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetParameterList(string name, object[] vals)
		{
			IQueryEvent queryEvent = new SetParameterListEvent(name, vals);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetParameterList(name, vals);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetProperties(object obj)
		{
			IQueryEvent queryEvent = new SetPropertiesEvent(obj);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetProperties(obj);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetAnsiString(int position, string val)
		{
			IQueryEvent queryEvent = new SetAnsiStringEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetAnsiString(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetAnsiString(string name, string val)
		{
			IQueryEvent queryEvent = new SetAnsiStringEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetAnsiString(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetBinary(int position, byte[] val)
		{
			IQueryEvent queryEvent = new SetBinaryEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetBinary(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetBinary(string name, byte[] val)
		{
			IQueryEvent queryEvent = new SetBinaryEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetBinary(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetBoolean(int position, bool val)
		{
			IQueryEvent queryEvent = new SetBooleanEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetBoolean(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetBoolean(string name, bool val)
		{
			IQueryEvent queryEvent = new SetBooleanEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetBoolean(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetByte(int position, byte val)
		{
			IQueryEvent queryEvent = new SetByteEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetByte(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetByte(string name, byte val)
		{
			IQueryEvent queryEvent = new SetByteEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetByte(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetCharacter(int position, char val)
		{
			IQueryEvent queryEvent = new SetCharacterEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetCharacter(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetCharacter(string name, char val)
		{
			IQueryEvent queryEvent = new SetCharacterEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetCharacter(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetDateTime(int position, DateTime val)
		{
			IQueryEvent queryEvent = new SetDateTimeEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetDateTime(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetDateTime(string name, DateTime val)
		{
			IQueryEvent queryEvent = new SetDateTimeEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetDateTime(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetDecimal(int position, decimal val)
		{
			IQueryEvent queryEvent = new SetDecimalEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetDecimal(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetDecimal(string name, decimal val)
		{
			IQueryEvent queryEvent = new SetDecimalEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetDecimal(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetDouble(int position, double val)
		{
			IQueryEvent queryEvent = new SetDoubleEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetDouble(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetDouble(string name, double val)
		{
			IQueryEvent queryEvent = new SetDoubleEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetDouble(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetEnum(int position, Enum val)
		{
			IQueryEvent queryEvent = new SetEnumEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetEnum(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetEnum(string name, Enum val)
		{
			IQueryEvent queryEvent = new SetEnumEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetEnum(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetInt16(int position, short val)
		{
			IQueryEvent queryEvent = new SetShortEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetInt16(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetInt16(string name, short val)
		{
			IQueryEvent queryEvent = new SetShortEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetInt16(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetInt32(int position, int val)
		{
			IQueryEvent queryEvent = new SetIntegerEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetInt32(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetInt32(string name, int val)
		{
			IQueryEvent queryEvent = new SetIntegerEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetInt32(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetInt64(int position, long val)
		{
			IQueryEvent queryEvent = new SetLongEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetInt64(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetInt64(string name, long val)
		{
			IQueryEvent queryEvent = new SetLongEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetInt64(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetSingle(int position, float val)
		{
			IQueryEvent queryEvent = new SetSingleEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetSingle(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetSingle(string name, float val)
		{
			IQueryEvent queryEvent = new SetSingleEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetSingle(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetString(int position, string val)
		{
			IQueryEvent queryEvent = new SetStringEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetString(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetString(string name, string val)
		{
			IQueryEvent queryEvent = new SetStringEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetString(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetTime(int position, DateTime val)
		{
			IQueryEvent queryEvent = new SetTimeEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetTime(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetTime(string name, DateTime val)
		{
			IQueryEvent queryEvent = new SetTimeEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetTime(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetTimestamp(int position, DateTime val)
		{
			IQueryEvent queryEvent = new SetTimestampEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetTimestamp(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetTimestamp(string name, DateTime val)
		{
			IQueryEvent queryEvent = new SetTimestampEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetTimestamp(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetGuid(int position, Guid val)
		{
			IQueryEvent queryEvent = new SetGuidEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetGuid(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetGuid(string name, Guid val)
		{
			IQueryEvent queryEvent = new SetGuidEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetGuid(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetEntity(int position, object val)
		{
			IQueryEvent queryEvent = new SetEntityEvent(position, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetEntity(position, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetEntity(string name, object val)
		{
			IQueryEvent queryEvent = new SetEntityEvent(name, val);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetEntity(name, val);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		public IQuery SetResultTransformer(IResultTransformer resultTransformer)
		{
			IQueryEvent queryEvent = new SetResultTransformerEvent(resultTransformer);
			foreach (IShard shard in shards)
			{
				if (shard.GetQueryById(queryId) != null)
				{
					shard.GetQueryById(queryId).SetResultTransformer(resultTransformer);
				}
				else
				{
					shard.AddQueryEvent(queryId, queryEvent);
				}
			}
			return this;
		}

		private IQuery GetOrEstablishSomeQuery()
		{
			IQuery query = SomeQuery;
			if (query == null)
			{
				IShard shard = shards[0];
				query = shard.EstablishQuery(this);
			}
			return query;
		}

		public IQuery SomeQuery
		{
			get
			{
				foreach (IShard shard in shards)
				{
					IQuery query = shard.GetQueryById(queryId);
					if (query != null)
					{
						return query;
					}
				}
				return null;
			}
		}

		public IEnumerable<T> Future<T>()
		{
			return GetOrEstablishSomeQuery().Future<T>();
		}

		public IFutureValue<T> FutureValue<T>()
		{
			return GetOrEstablishSomeQuery().FutureValue<T>();
		}

		public string QueryString
		{
			get { return GetOrEstablishSomeQuery().QueryString; }
		}

		public IType[] ReturnTypes
		{
			get { return GetOrEstablishSomeQuery().ReturnTypes; }
		}

		public string[] ReturnAliases
		{
			get { return GetOrEstablishSomeQuery().ReturnAliases; }
		}

		public string[] NamedParameters
		{
			get { return GetOrEstablishSomeQuery().NamedParameters; }
		}

		public QueryId QueryId
		{
            get { return this.queryId; }
		}

		public IQueryFactory QueryFactory
		{
            get { return this.queryFactory; }
		}

		private class UniqueResultShardOperation<T> : IShardOperation<T>
		{
			private IShardedQuery shardedQuery;
			private QueryId queryId;

			public UniqueResultShardOperation(QueryId queryId, IShardedQuery shardedQuery)
			{
				this.queryId = queryId;
				this.shardedQuery = shardedQuery;
			}

			public T Execute(IShard shard)
			{
				shard.EstablishQuery(shardedQuery);
                return (T) shard.UniqueResult(this.queryId);
			}

			public string OperationName
			{
				get { return "uniqueResult()"; }
			}
		}

		private class ListShardOperation<T> : IShardOperation<T>
		{
			private IShardedQuery shardedQuery;
			private QueryId queryId;

			public ListShardOperation(QueryId queryId, IShardedQuery shardedQuery)
			{
				this.queryId = queryId;
				this.shardedQuery = shardedQuery;
			}

			public T Execute(IShard shard)
			{
				shard.EstablishQuery(shardedQuery);
                return (T) shard.List(this.queryId);
			}

			public string OperationName
			{
				get { return "list()"; }
			}
		}
	}
}