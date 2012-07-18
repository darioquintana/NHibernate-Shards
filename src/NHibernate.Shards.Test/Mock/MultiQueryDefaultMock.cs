using System;
using System.Collections;
using NHibernate.Transform;
using NHibernate.Type;

namespace NHibernate.Shards.Test.Mock
{
    public class MultiQueryDefaultMock: IMultiQuery
    {
        public virtual IList List()
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery Add(System.Type resultGenericListType, IQuery query)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery Add<T>(IQuery query)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery Add<T>(string key, IQuery query)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery Add<T>(string key, string hql)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery Add<T>(string hql)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery AddNamedQuery<T>(string queryName)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery AddNamedQuery<T>(string key, string queryName)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery Add(string key, IQuery query)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery Add(IQuery query)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery Add(string key, string hql)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery Add(string hql)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery AddNamedQuery(string queryName)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery AddNamedQuery(string key, string queryName)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetCacheable(bool cacheable)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetCacheRegion(string region)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetForceCacheRefresh(bool forceCacheRefresh)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetTimeout(int timeout)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetParameter(string name, object val, IType type)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetParameter(string name, object val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetParameterList(string name, ICollection vals, IType type)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetParameterList(string name, ICollection vals)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetAnsiString(string name, string val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetBinary(string name, byte[] val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetBoolean(string name, bool val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetByte(string name, byte val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetCharacter(string name, char val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetDateTime(string name, DateTime val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetDecimal(string name, decimal val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetDouble(string name, double val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetEntity(string name, object val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetEnum(string name, Enum val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetInt16(string name, short val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetInt32(string name, int val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetInt64(string name, long val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetSingle(string name, float val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetString(string name, string val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetGuid(string name, Guid val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetTime(string name, DateTime val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetTimestamp(string name, DateTime val)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetFlushMode(FlushMode mode)
        {
            throw new NotSupportedException();
        }

        public virtual IMultiQuery SetResultTransformer(IResultTransformer transformer)
        {
            throw new NotSupportedException();
        }

        public virtual object GetResult(string key)
        {
            throw new NotSupportedException();
        }
    }
}
