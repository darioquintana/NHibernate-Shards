using System;
using System.Collections.Generic;

using Iesi.Collections.Generic;

namespace NHibernate.Shards.Test
{
    public class ShardDefaultMock: IShard
    {
        public virtual ISessionFactory SessionFactory
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ISession Session
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ICollection<ShardId> ShardIds
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ISession EstablishSession()
        {
            throw new NotSupportedException();
        }

        public virtual bool Contains(object entity)
        {
            throw new NotSupportedException();
        }

        public virtual NHibernate.Engine.ISessionFactoryImplementor SessionFactoryImplementor
        {
            get { throw new NotSupportedException(); }
        }

        public virtual ICriteria EstablishCriteria(NHibernate.Shards.Criteria.IShardedCriteria shardedCriteria)
        {
            throw new NotSupportedException();
        }

        public virtual IQuery EstablishQuery(NHibernate.Shards.Query.IShardedQuery shardedQuery)
        {
            throw new NotSupportedException();
        }
    }
}
