namespace NHibernate.Shards.Query
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NHibernate.Shards.Engine;

    public class ShardedExpressionQueryImpl : ShardedQueryImpl
    {
        private IQueryExpression queryExpression;

        public ShardedExpressionQueryImpl(IShardedSessionImplementor session, IQueryExpression queryExpression) 
            : base(session, s => s.GetSessionImplementation().CreateQuery(queryExpression))
        {
            this.queryExpression = queryExpression;
        }

        public IQueryExpression QueryExpression
        {
            get { return this.queryExpression; }
        }

        public override IList List()
        {
            var result = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(this.queryExpression.Type));
            List(result);
            return result;
        }
    }
}