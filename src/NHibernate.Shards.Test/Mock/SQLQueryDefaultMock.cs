using System;
using NHibernate.Type;

namespace NHibernate.Shards.Test.Mock
{
    public class SQLQueryDefaultMock: QueryDefaultMock, ISQLQuery
    {
        public virtual ISQLQuery AddEntity(string entityName)
        {
            throw new NotSupportedException();
        }

        public virtual ISQLQuery AddEntity(string alias, string entityName)
        {
            throw new NotSupportedException();
        }

        public virtual ISQLQuery AddEntity(string alias, string entityName, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual ISQLQuery AddEntity(System.Type entityClass)
        {
            throw new NotSupportedException();
        }

        public virtual ISQLQuery AddEntity(string alias, System.Type entityClass)
        {
            throw new NotSupportedException();
        }

        public virtual ISQLQuery AddEntity(string alias, System.Type entityClass, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual ISQLQuery AddJoin(string alias, string path)
        {
            throw new NotSupportedException();
        }

        public virtual ISQLQuery AddJoin(string alias, string path, LockMode lockMode)
        {
            throw new NotSupportedException();
        }

        public virtual ISQLQuery AddScalar(string columnAlias, IType type)
        {
            throw new NotSupportedException();
        }

        public virtual ISQLQuery SetResultSetMapping(string name)
        {
            throw new NotSupportedException();
        }
    }
}
