using System;
using System.Collections;
using NHibernate.SqlCommand;
using NHibernate.Type;

namespace NHibernate.Shards.Util
{
	public class InterceptorList : IInterceptor
	{
		public bool OnLoad(object entity, object id, object[] state, string[] propertyNames, IType[] types)
		{
			throw new NotImplementedException();
		}

		public bool OnFlushDirty(object entity, object id, object[] currentState, object[] previousState, string[] propertyNames, IType[] types)
		{
			throw new NotImplementedException();
		}

		public bool OnSave(object entity, object id, object[] state, string[] propertyNames, IType[] types)
		{
			throw new NotImplementedException();
		}

		public void OnDelete(object entity, object id, object[] state, string[] propertyNames, IType[] types)
		{
			throw new NotImplementedException();
		}

		public void OnCollectionRecreate(object collection, object key)
		{
			throw new NotImplementedException();
		}

		public void OnCollectionRemove(object collection, object key)
		{
			throw new NotImplementedException();
		}

		public void OnCollectionUpdate(object collection, object key)
		{
			throw new NotImplementedException();
		}

		public void PreFlush(ICollection entities)
		{
			throw new NotImplementedException();
		}

		public void PostFlush(ICollection entities)
		{
			throw new NotImplementedException();
		}

		public bool? IsTransient(object entity)
		{
			throw new NotImplementedException();
		}

		public int[] FindDirty(object entity, object id, object[] currentState, object[] previousState, string[] propertyNames, IType[] types)
		{
			throw new NotImplementedException();
		}

		public object Instantiate(string entityName, EntityMode entityMode, object id)
		{
			throw new NotImplementedException();
		}

		public string GetEntityName(object entity)
		{
			throw new NotImplementedException();
		}

		public object GetEntity(string entityName, object id)
		{
			throw new NotImplementedException();
		}

		public void AfterTransactionBegin(ITransaction tx)
		{
			throw new NotImplementedException();
		}

		public void BeforeTransactionCompletion(ITransaction tx)
		{
			throw new NotImplementedException();
		}

		public void AfterTransactionCompletion(ITransaction tx)
		{
			throw new NotImplementedException();
		}

		public SqlString OnPrepareStatement(SqlString sql)
		{
			throw new NotImplementedException();
		}

		public void SetSession(ISession session)
		{
			throw new NotImplementedException();
		}
	}
}