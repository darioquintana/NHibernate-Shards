//using System;
//using NHibernate.Criterion;
//using System.Collections.Generic;
//using log4net;
//using NHibernate.Engine;
//using NHibernate.Shards.Strategy.Exit;

//namespace NHibernate.Shards.Criteria
//{
//    public class ExitOperationsCriteriaCollector
//    {
//      // maximum number of results requested by the client
//      private int maxResults;// = null;

//      // index of the first result requested by the client
//      private int firstResult;// = null;

//      // Distinct operation applied to the Criteria
//        private Distinct distinct;// = null;

//      // Average Projection operation applied to the Criteria
//        private AggregateProjection avgProjection;// = null;

//      // Aggregate Projecton operation applied to the Criteria
//        private AggregateProjection aggregateProjection;// = null;

//      // Row Count Projection operation applied to the Criteria
//      private RowCountProjection rowCountProjection;

//      // The Session Factory Implementor with which the Criteria is associated
//      private ISessionFactoryImplementor sessionFactoryImplementor;

//      // Order operations applied to the Criteria
//      private List<InMemoryOrderBy> orders = Lists.newArrayList();

//      // Our friendly neighborhood logger private readonly Log log = LogFactory.getLog(getClass());
//      private readonly ILog log = LogManager.GetLogger(getClass());  
//      /**
//       * Sets the maximum number of results requested by the client
//       *
//       * @param maxResults maximum number of results requested by the client
//       * @return this
//       */
//      public IExitOperationsCollector SetMaxResults(int maxResults) {
//        this.maxResults = maxResults;
//        return this as IExitOperationsCollector;
//      }

//      /**
//       * Sets the index of the first result requested by the client
//       *
//       * @param firstResult index of the first result requested by the client
//       * @return this
//       */
//      public IExitOperationsCollector SetFirstResult(int firstResult) {
//        this.firstResult = firstResult;
//        return this as IExitOperationsCollector;
//      }

//      /**
//       * Adds the given projection.
//       *
//       * @param projection the projection to add
//       * @return this
//       */
//      public IExitOperationsCollector AddProjection(IProjection projection) {
//        if (projection is Distinct) {
//          distinct = (Distinct)projection;
//          // TODO(maulik) Distinct doesn't work yet
//          log.Error("Distinct is not ready yet");
//          throw new UnsupportedOperationException();
//        } else if(projection is RowCountProjection) {
//          rowCountProjection = (RowCountProjection) projection;
//        } else if(projection is AggregateProjection) {
//          if (projection.ToString().ToLower().StartsWith("avg")) {
//            avgProjection = (AggregateProjection) projection;
//          } else {
//            aggregateProjection = (AggregateProjection) projection;
//          }
//        } else {
//          log.Error("Adding an unsupported Projection: " + projection.getClass().getName());
//          throw new UnsupportedOperationException();
//        }
//        return this as IExitOperationsCollector;
//      }

//      /**
//       * Add the given Order
//       *
//       * @param associationPath the association path leading to the object to which
//       * this order clause applies - null if the order clause applies to the top
//       * level object
//       * @param order the order to add
//       * @return this
//       */
//      public IExitOperationsCollector AddOrder(string associationPath, Order order) {
//        orders.Add(new InMemoryOrderBy(associationPath, order));
//        return this as IExitOperationsCollector;
//      }

//      public List<Object> Apply(List<Object> result) {
//        /**
//         * Herein lies the glory
//         *
//         * hibernate has done as much as it can, we're going to have to deal with
//         * the rest in memory.
//         *
//         * The heirarchy of operations is this so far:
//         * Distinct
//         * Order
//         * FirstResult
//         * MaxResult
//         * RowCount
//         * Average
//         * Min/Max/Sum
//         */

//        // ordering of the following operations *really* matters!
//        if (distinct != null) {
//          result = new DistinctExitOperation(distinct).Apply(result);
//        }

//        // not clear to me why we need to create an OrderExitOperation
//        // are we even taking advantage of the fact that it implements the
//        // ExitOperation interface?
//        OrderExitOperation op = new OrderExitOperation(orders);
//        result = op.Apply(result);

//        if (firstResult != null) {
//          result = new FirstResultExitOperation(firstResult).Apply(result);
//        }
//        if (maxResults != null) {
//          result = new MaxResultsExitOperation(maxResults).Apply(result);
//        }

//        ProjectionExitOperationFactory factory =
//            ProjectionExitOperationFactory.getFactory();

//        if (rowCountProjection != null) {
//          result = factory.getProjectionExitOperation(rowCountProjection, sessionFactoryImplementor).apply(result);
//        }

//        if (avgProjection != null) {
//          result = new AvgResultsExitOperation().Apply(result);
//        }

//        // min, max, sum
//        if (aggregateProjection != null) {
//          result = factory.getProjectionExitOperation(aggregateProjection, sessionFactoryImplementor).apply(result);
//        }
//        return result;
//      }

//      /**
//       * Sets the session factory implementor
//       * @param sessionFactoryImplementor the session factory implementor to set
//       */
//      public void setSessionFactory(ISessionFactoryImplementor sessionFactoryImplementor) {
//        this.sessionFactoryImplementor = sessionFactoryImplementor;
//      }

//      int getMaxResults() {
//        return maxResults;
//      }

//      int getFirstResult() {
//        return firstResult;
//      }

//    }
//}
