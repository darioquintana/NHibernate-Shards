using NHibernate.Shards.Engine;

namespace NHibernate.Shards.Query
{
    public class ShardedSQLQueryImpl: ShardedQueryImpl, IShardedSQLQuery
    {
        public ShardedSQLQueryImpl(IShardedSessionImplementor session, string queryString)
            : base(session, s => s.CreateSQLQuery(queryString))
        {}

        #region ISQLQuery Members

        public ISQLQuery AddEntity(string alias, System.Type entityClass, LockMode lockMode)
        {
            ApplyActionToShards(q => ((ISQLQuery)q).AddEntity(alias, entityClass, lockMode));
            return this;
        }

        public ISQLQuery AddEntity(string alias, System.Type entityClass)
        {
            ApplyActionToShards(q => ((ISQLQuery)q).AddEntity(alias, entityClass));
            return this;
        }

        public ISQLQuery AddEntity(System.Type entityClass)
        {
            ApplyActionToShards(q => ((ISQLQuery)q).AddEntity(entityClass));
            return this;
        }

        public ISQLQuery AddEntity(string alias, string entityName, LockMode lockMode)
        {
            ApplyActionToShards(q => ((ISQLQuery)q).AddEntity(alias, entityName, lockMode));
            return this;
        }

        public ISQLQuery AddEntity(string alias, string entityName)
        {
            ApplyActionToShards(q => ((ISQLQuery)q).AddEntity(alias, entityName));
            return this;
        }

        public ISQLQuery AddEntity(string entityName)
        {
            ApplyActionToShards(q => ((ISQLQuery)q).AddEntity(entityName));
            return this;
        }

        public ISQLQuery AddJoin(string alias, string path, LockMode lockMode)
        {
            ApplyActionToShards(q => ((ISQLQuery)q).AddJoin(alias, path, lockMode));
            return this;
        }

        public ISQLQuery AddJoin(string alias, string path)
        {
            ApplyActionToShards(q => ((ISQLQuery)q).AddJoin(alias, path));
            return this;
        }

        public ISQLQuery AddScalar(string columnAlias, NHibernate.Type.IType type)
        {
            ApplyActionToShards(q => ((ISQLQuery)q).AddScalar(columnAlias, type));
            return this;
        }

        public ISQLQuery SetResultSetMapping(string name)
        {
            ApplyActionToShards(q => ((ISQLQuery)q).SetResultSetMapping(name));
            return this;
        }

        #endregion
    }
}
