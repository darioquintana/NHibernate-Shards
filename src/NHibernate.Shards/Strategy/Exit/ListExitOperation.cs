using System;
using System.Collections.Generic;
using System.Linq;

namespace NHibernate.Shards.Strategy.Exit
{
    /// <summary>
    /// Represents method object that performs postprocessing on results that have
    /// been collected from shards.
    /// </summary>
    public class ListExitOperation
    {
        /// <summary>
        /// Maximum number of results requested by the client.
        /// </summary>
        public int? MaxResults { get; private set; }

        /// <summary>
        /// Index of the first result requested by the client.
        /// </summary>
        public int FirstResult { get; private set; }

        /// <summary>
        /// Indication whether client requests removal of duplicate results.
        /// </summary>
        public bool Distinct { get; private set; }

        /// <summary>
        /// Optional aggregation function to be applied to the results. 
        /// </summary>
        public AggregationFunc Aggregation { get; private set; }

        /// <summary>
        /// Optional sort order to be applied to the results.
        /// </summary>
        public IComparer<object> Order { get; private set; }

        /// <summary>
        /// Creates new <see cref="ListExitOperation"/> instance.
        /// </summary>
        /// <param name="maxResults">Maximum number of results requested by the client.</param>
        /// <param name="firstResult">Index of the first result requested by the client.</param>
        /// <param name="distinct">Indication whether client requests removal of duplicate results.</param>
        /// <param name="aggregation">Optional aggregation function to be applied to the results. </param>
        /// <param name="order">Optional sort order to be applied to the results.</param>
        public ListExitOperation(
            int? maxResults, 
            int firstResult, 
            bool distinct, 
            AggregationFunc aggregation, 
            IComparer<object> order)
        {
            this.MaxResults = maxResults;
            this.FirstResult = firstResult;
            this.Distinct = distinct;
            this.Aggregation = aggregation;
            this.Order = order;
        }

        /// <summary>
        /// Transforms collected results from shards into final result for <c>List</c> 
        /// operations on sharded <see cref="IQuery"/> or <see cref="ICriteria"/> 
        /// implementations.
        /// </summary>
        /// <param name="input">Results that have been collected from individual shards.</param>
        /// <returns>Merged and post-processed output from shards.</returns>
        public IEnumerable<T> Execute<T>(IEnumerable<T> input)
        {
            var result = input;

            if (System.Type.GetTypeCode(typeof(T)) == TypeCode.Object)
            {
                result = result.Where(i => i != null);
            }

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
            if (this.Distinct)
            {
                result = result.Distinct();
            }
            if (this.Order != null)
            {
                result = result.OrderBy(x => x, this.Order);
            }
            if (this.FirstResult > 0)
            {
                result = result.Skip(this.FirstResult);
            }
            if (this.MaxResults.HasValue)
            {
                result = result.Take(this.MaxResults.Value);
            }

            if (this.Aggregation != null)
            {
                var scalar = this.Aggregation(result);
                return new[] {(T)scalar};
            }

            return result;
        }
    }
}
