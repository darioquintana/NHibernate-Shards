using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Criterion;
using NHibernate.Engine;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Criteria
{
	
	/**
	 * Implements the ExitOperationsCollector interface for Critierias
	 *
	 * @author Maulik Shah
	 */
	public class ExitOperationsCriteriaCollector : IExitOperationsCollector
	{
		// Our friendly neighborhood logger
    	private static readonly IInternalLogger Log = LoggerProvider.LoggerFor(typeof(ExitOperationsCriteriaCollector));

		// maximum number of results requested by the client
		private int? maxResults;

		// index of the first result requested by the client
		private int? firstResult;

		// Distinct operation applied to the Criteria
        private Distinct distinct;

		// Average Projection operation applied to the Criteria
		private AggregateProjection avgProjection;

		// Aggregate Projecton operation applied to the Criteria
		private AggregateProjection aggregateProjection;

		// Row Count Projection operation applied to the Criteria
		private RowCountProjection rowCountProjection;

		// The Session Factory Implementor with which the Criteria is associated
		private ISessionFactoryImplementor sessionFactoryImplementor;

		// Order operations applied to the Criteria
		private readonly IList<Order> orders = new List<Order>();

		/**
		 * Sets the maximum number of results requested by the client
		 *
		 * @param maxResults maximum number of results requested by the client
		 * @return this
		 */
		public IExitOperationsCollector MaxResults(int maxResultsValue)
		{
			maxResults = maxResultsValue;
			return this;
		}

		/**
		 * Sets the index of the first result requested by the client
		 *
		 * @param firstResult index of the first result requested by the client
		 * @return this
		 */
		public IExitOperationsCollector FirstResult(int firstResultValue)
		{
            this.firstResult = firstResultValue;
			return this;
		}

		/**
		 * Adds the given projection.
		 *
		 * @param projection the projection to add
		 * @return this
		 */
		public IExitOperationsCollector AddProjection(IProjection projection)
		{
			if (projection.GetType().IsAssignableFrom(distinct.GetType()))
			{
            	this.distinct = (Distinct) projection;
				//TODO: Distinct doesn't work yet
            	Log.Error("Distinct is not ready yet");
				throw new NotSupportedException();
			}
			if (projection.GetType().IsAssignableFrom(rowCountProjection.GetType()))
			{
				rowCountProjection = (RowCountProjection) projection;
			}
			if (projection.GetType().IsAssignableFrom(aggregateProjection.GetType()))
			{
				if (projection.ToString().ToLower().StartsWith("avg"))
				{
                    this.avgProjection = (AggregateProjection)projection;
				}
				else
				{
                    this.aggregateProjection = (AggregateProjection)projection;
				}
			}
			else
			{
            	Log.Error("Adding an unsupported Projection: " + projection.GetType().Name);
				throw new NotSupportedException();
			}

			return this;
		}

		/**
		 * Add the given Order
		 *
		 * @param order the order to add
		 * @return this
		 */
		public IExitOperationsCollector AddOrder(Order order)
		{
            this.orders.Add(order);
			return this;
		}

		public IList Apply(IList result)
		{
			/**
			 * Herein lies the glory
			 *
			 * hibernate has done as much as it can, we're going to have to deal with
			 * the rest in memory.
			 *
			 * The heirarchy of operations is this so far:
			 * Distinct
			 * Order
			 * FirstResult
			 * MaxResult
			 * RowCount
			 * Average
			 * Min/Max/Sum
			 */

			// ordering of the following operations *really* matters!
			if (distinct != null)
			{
				result = new DistinctExitOperation(distinct).Apply(result);
			}
			foreach (Order order in orders)
			{
				result = new OrderExitOperation(order).Apply(result);
			}
			if (firstResult != null)
			{
				result = new FirstResultExitOperation((int) firstResult).Apply(result);
			}
			if (maxResults != null)
			{
				result = new MaxResultsExitOperation((int) maxResults).Apply(result);
			}
			ProjectionExitOperationFactory factory = ProjectionExitOperationFactory.GetFactory();

			if (rowCountProjection != null)
			{
				result = factory.GetProjectionExitOperation(rowCountProjection, sessionFactoryImplementor).Apply(result);
			}
			if (avgProjection != null)
			{
				result = new AvgResultsExitOperation().Apply(result);
			}
			// min, max, sum
			if (aggregateProjection != null)
			{
				result = factory.GetProjectionExitOperation(aggregateProjection, sessionFactoryImplementor).Apply(result);
			}

			return result;
		}

		/**
		 * Sets the session factory implementor
		 * @param sessionFactoryImplementor the session factory implementor to set
		 */
		public void SetSessionFactory(ISessionFactoryImplementor sessionFactoryImplementorValue)
		{
            this.sessionFactoryImplementor = sessionFactoryImplementorValue;
		}
	}
}