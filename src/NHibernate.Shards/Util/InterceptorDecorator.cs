using System.Collections;
using NHibernate.SqlCommand;
using NHibernate.Type;

namespace NHibernate.Shards.Util
{
    public class InterceptorDecorator:IInterceptor
    {
        private readonly IInterceptor delegateInterceptor;

        public InterceptorDecorator(IInterceptor delegateInterceptor)
        {
            this.delegateInterceptor = delegateInterceptor;
        }      

        public virtual bool OnLoad(object entity, object id, object[] state, string[] propertyNames, IType[] types)
        {
            return this.delegateInterceptor.OnLoad(entity, id, state, propertyNames, types);
        }

        public virtual bool OnFlushDirty(object entity, object id, object[] currentState, object[] previousState, string[] propertyNames, IType[] types)
        {
            return this.delegateInterceptor.OnFlushDirty(entity, id, currentState, previousState, propertyNames, types);
        }

        public virtual bool OnSave(object entity, object id, object[] state, string[] propertyNames, IType[] types)
        {
            return this.delegateInterceptor.OnSave(entity, id, state, propertyNames, types);
        }

        public virtual void OnDelete(object entity, object id, object[] state, string[] propertyNames, IType[] types)
        {
            this.delegateInterceptor.OnDelete(entity, id, state, propertyNames, types);
        }

        public virtual void OnCollectionRecreate(object collection, object key)
        {
            this.delegateInterceptor.OnCollectionRecreate(collection, key);
        }

        public virtual void OnCollectionRemove(object collection, object key)
        {
            this.delegateInterceptor.OnCollectionRemove(collection, key);            
        }

        public virtual void OnCollectionUpdate(object collection, object key)
        {
            this.delegateInterceptor.OnCollectionUpdate(collection, key);
        }

        public virtual void PreFlush(ICollection entities)
        {
            this.delegateInterceptor.PreFlush(entities);
        }

        public virtual void PostFlush(ICollection entities)
        {
            this.delegateInterceptor.PostFlush(entities);
        }

        public virtual bool? IsTransient(object entity)
        {
            return this.delegateInterceptor.IsTransient(entity);
        }

        public virtual int[] FindDirty(object entity, object id, object[] currentState, object[] previousState, string[] propertyNames, IType[] types)
        {
            return this.delegateInterceptor.FindDirty(entity, id, currentState, previousState, propertyNames, types);
        }

        public virtual object Instantiate(string entityName, EntityMode entityMode, object id)
        {
            return this.delegateInterceptor.Instantiate(entityName, entityMode, id);
        }

        public virtual string GetEntityName(object entity)
        {
            return this.delegateInterceptor.GetEntityName(entity);
        }

        public virtual object GetEntity(string entityName, object id)
        {
            return this.delegateInterceptor.GetEntity(entityName, id);
        }

        public virtual void AfterTransactionBegin(ITransaction tx)
        {
            this.delegateInterceptor.AfterTransactionBegin(tx);
        }

        public virtual void BeforeTransactionCompletion(ITransaction tx)
        {
            this.delegateInterceptor.BeforeTransactionCompletion(tx);
        }

        public virtual void AfterTransactionCompletion(ITransaction tx)
        {
            this.delegateInterceptor.AfterTransactionCompletion(tx);
        }

        public virtual SqlString OnPrepareStatement(SqlString sql)
        {
            return this.delegateInterceptor.OnPrepareStatement(sql);            
        }

        public virtual void SetSession(ISession session)
        {
            this.delegateInterceptor.SetSession(session);
        }
    }
}
