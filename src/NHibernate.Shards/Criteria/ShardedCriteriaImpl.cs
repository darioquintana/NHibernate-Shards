using System;
using System.Collections;
using NHibernate.Criterion;
using System.Collections.Generic;
using NHibernate.SqlCommand;
using NHibernate.Transform;

namespace NHibernate.Shards.Criteria
{
	public class ShardedCriteriaImpl : IShardedCriteria
	{
		public object Clone()
		{
			throw new NotImplementedException();
		}

		public ICriteria SetProjection(IProjection projection)
		{
			throw new NotImplementedException();
		}

		public ICriteria Add(ICriterion expression)
		{
			throw new NotImplementedException();
		}

		public ICriteria AddOrder(Order order)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetFetchMode(string associationPath, FetchMode mode)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetLockMode(LockMode lockMode)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetLockMode(string alias, LockMode lockMode)
		{
			throw new NotImplementedException();
		}

		public ICriteria CreateAlias(string associationPath, string alias)
		{
			throw new NotImplementedException();
		}

		public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType)
		{
			throw new NotImplementedException();
		}

		public ICriteria CreateCriteria(string associationPath)
		{
			throw new NotImplementedException();
		}

		public ICriteria CreateCriteria(string associationPath, JoinType joinType)
		{
			throw new NotImplementedException();
		}

		public ICriteria CreateCriteria(string associationPath, string alias)
		{
			throw new NotImplementedException();
		}

		public ICriteria CreateCriteria(string associationPath, string alias, JoinType joinType)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetResultTransformer(IResultTransformer resultTransformer)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetMaxResults(int maxResults)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetFirstResult(int firstResult)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetFetchSize(int fetchSize)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetTimeout(int timeout)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetCacheable(bool cacheable)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetCacheRegion(string cacheRegion)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetComment(string comment)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetFlushMode(FlushMode flushMode)
		{
			throw new NotImplementedException();
		}

		public ICriteria SetCacheMode(CacheMode cacheMode)
		{
			throw new NotImplementedException();
		}

		public IList List()
		{
			throw new NotImplementedException();
		}

		public object UniqueResult()
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

		public void List(IList results)
		{
			throw new NotImplementedException();
		}

		public IList<T> List<T>()
		{
			throw new NotImplementedException();
		}

		public T UniqueResult<T>()
		{
			throw new NotImplementedException();
		}

		public void ClearOrders()
		{
			throw new NotImplementedException();
		}

		public ICriteria GetCriteriaByPath(string path)
		{
			throw new NotImplementedException();
		}

		public ICriteria GetCriteriaByAlias(string alias)
		{
			throw new NotImplementedException();
		}

		public System.Type GetRootEntityTypeIfAvailable()
		{
			throw new NotImplementedException();
		}

		public string Alias
		{
			get { throw new NotImplementedException(); }
		}

		public CriteriaId CriteriaId
		{
			get { throw new NotImplementedException(); }
		}

		public ICriteriaFactory CriteriaFactory
		{
			get { throw new NotImplementedException(); }
		}
	}
}
