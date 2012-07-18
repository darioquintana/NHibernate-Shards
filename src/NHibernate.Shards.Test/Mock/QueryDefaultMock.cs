using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using NHibernate.Transform;
using NHibernate.Type;

namespace NHibernate.Shards.Test.Mock
{
    public class QueryDefaultMock: IQuery
    {
        public virtual IEnumerable Enumerable()
        {
            throw new NotSupportedException();
        }

        public virtual IEnumerable<T> Enumerable<T>()
        {
            throw new NotSupportedException();
        }

        public virtual IList List()
        {
            throw new NotSupportedException();
        }

        public virtual void List(IList results)
        {
            throw new NotSupportedException();
        }

        public virtual IList<T> List<T>()
        {
            throw new NotSupportedException();
        }

        public virtual object UniqueResult()
        {
            throw new NotSupportedException();
        }

        public virtual T UniqueResult<T>()
        {
            throw new NotSupportedException();
        }

        public virtual int ExecuteUpdate()
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetMaxResults(int maxResults)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetFirstResult(int firstResult)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetReadOnly(bool readOnly)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetCacheable(bool cacheable)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetCacheRegion(string cacheRegion)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetTimeout(int timeout)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetFetchSize(int fetchSize)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetLockMode(string alias, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetComment(string comment)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetFlushMode(FlushMode flushMode)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetCacheMode(CacheMode cacheMode)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetParameter(int position, object val, IType type)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetParameter(string name, object val, IType type)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetParameter<T>(int position, T val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetParameter<T>(string name, T val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetParameter(int position, object val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetParameter(string name, object val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetParameterList(string name, IEnumerable vals, IType type)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetParameterList(string name, IEnumerable vals)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetProperties(object obj)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetAnsiString(int position, string val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetAnsiString(string name, string val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetBinary(int position, byte[] val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetBinary(string name, byte[] val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetBoolean(int position, bool val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetBoolean(string name, bool val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetByte(int position, byte val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetByte(string name, byte val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetCharacter(int position, char val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetCharacter(string name, char val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetDateTime(int position, DateTime val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetDateTime(string name, DateTime val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetDecimal(int position, decimal val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetDecimal(string name, decimal val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetDouble(int position, double val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetDouble(string name, double val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetEnum(int position, Enum val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetEnum(string name, Enum val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetInt16(int position, short val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetInt16(string name, short val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetInt32(int position, int val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetInt32(string name, int val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetInt64(int position, long val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetInt64(string name, long val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetSingle(int position, float val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetSingle(string name, float val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetString(int position, string val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetString(string name, string val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetTime(int position, DateTime val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetTime(string name, DateTime val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetTimestamp(int position, DateTime val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetTimestamp(string name, DateTime val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetGuid(int position, Guid val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetGuid(string name, Guid val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetEntity(int position, object val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetEntity(string name, object val)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery SetResultTransformer(IResultTransformer resultTransformer)
        {
            throw new NotSupportedException();
        }

        public virtual IEnumerable<T> Future<T>()
        {
            throw new NotSupportedException();
        }

        public virtual IFutureValue<T> FutureValue<T>()
        {
            throw new NotSupportedException();
        }

        public virtual string QueryString
        {
            get { throw new NotSupportedException(); }
        }

        public virtual IType[] ReturnTypes
        {
            get { throw new NotSupportedException(); }
        }

        public virtual string[] ReturnAliases
        {
            get { throw new NotSupportedException(); }
        }

        public virtual string[] NamedParameters
        {
            get { throw new NotSupportedException(); }
        }
    }
}
