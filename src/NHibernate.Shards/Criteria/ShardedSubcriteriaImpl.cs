using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using NHibernate.Transform;

namespace NHibernate.Shards.Criteria
{
	internal class ShardedSubcriteriaImpl : IShardedSubcriteria
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

		public IShardedCriteria GetParentCriteria()
		{
			throw new NotImplementedException();
		}

		#region Miembros de IShardedSubcriteria

		IShardedCriteria IShardedSubcriteria.GetParentCriteria()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Miembros de ICriteria

		ICriteria ICriteria.Add(ICriterion expression)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.AddOrder(Order order)
		{
			throw new NotImplementedException();
		}

		string ICriteria.Alias
		{
			get { throw new NotImplementedException(); }
		}

		void ICriteria.ClearOrders()
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.CreateAlias(string associationPath, string alias, JoinType joinType)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.CreateAlias(string associationPath, string alias)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.CreateCriteria(string associationPath, string alias, JoinType joinType)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.CreateCriteria(string associationPath, string alias)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.CreateCriteria(string associationPath, JoinType joinType)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.CreateCriteria(string associationPath)
		{
			throw new NotImplementedException();
		}

		IEnumerable<T> ICriteria.Future<T>()
		{
			throw new NotImplementedException();
		}

		IFutureValue<T> ICriteria.FutureValue<T>()
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.GetCriteriaByAlias(string alias)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.GetCriteriaByPath(string path)
		{
			throw new NotImplementedException();
		}

		System.Type ICriteria.GetRootEntityTypeIfAvailable()
		{
			throw new NotImplementedException();
		}

		IList<T> ICriteria.List<T>()
		{
			throw new NotImplementedException();
		}

		void ICriteria.List(IList results)
		{
			throw new NotImplementedException();
		}

		IList ICriteria.List()
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetCacheMode(CacheMode cacheMode)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetCacheRegion(string cacheRegion)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetCacheable(bool cacheable)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetComment(string comment)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetFetchMode(string associationPath, FetchMode mode)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetFetchSize(int fetchSize)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetFirstResult(int firstResult)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetFlushMode(FlushMode flushMode)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetLockMode(string alias, LockMode lockMode)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetLockMode(LockMode lockMode)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetMaxResults(int maxResults)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetProjection(IProjection projection)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetResultTransformer(IResultTransformer resultTransformer)
		{
			throw new NotImplementedException();
		}

		ICriteria ICriteria.SetTimeout(int timeout)
		{
			throw new NotImplementedException();
		}

		T ICriteria.UniqueResult<T>()
		{
			throw new NotImplementedException();
		}

		object ICriteria.UniqueResult()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Miembros de ICloneable
		#endregion
		object ICloneable.Clone()
		{
			throw new NotImplementedException();
		}

		public interface ISubcriteriaRegistrar
		{
			void EstablishSubcriteria(ICriteria parentCriteria, ISubCriteriaFactory subcriteriaFactory);
		}
	}
}
