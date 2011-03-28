using System;
using NHibernate.Criterion;
using NHibernate.Engine;

namespace NHibernate.Shards.Strategy.Exit
{
    public class ProjectionExitOperationFactory
    {
        private static ProjectionExitOperationFactory projectionExitOperationFactory = new ProjectionExitOperationFactory();

        private ProjectionExitOperationFactory()
        {

        }

        public static ProjectionExitOperationFactory GetFactory()
        {
            return projectionExitOperationFactory;
        }

        public IProjectionExitOperation GetProjectionExitOperation(IProjection projection, ISessionFactory sessionFactory)
        {
            if (projection.GetType().IsAssignableFrom(typeof(RowCountExitOperation)))
            {
                return new RowCountExitOperation(projection);
            }
            if (projection.GetType().IsAssignableFrom(typeof(AggregateProjection)))
            {
                return new AggregateExitOperation(projection);
            }
            throw new NotSupportedException("This project is not supported: " + projection.GetType());
        }
    }
}