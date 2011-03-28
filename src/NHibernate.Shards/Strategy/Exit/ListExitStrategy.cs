using System.Collections.Generic;
using System.Linq;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Exit
{
    /// <summary>
    /// Threadsafe ExistStrategy that concatenates all the lists that are added.
    /// </summary>
    public class ListExitStrategy<T> : IListExitStrategy<T>
    {
        // List instance to which final results are added
        private IEnumerable<T> result;

        // maximum number of results requested by the client
        private ListExitOperation exitOperation;

        public ListExitStrategy(ListExitOperation exitOperation)
        {
            Preconditions.CheckNotNull(exitOperation);
            this.exitOperation = exitOperation;
        }

        /// <summary>
        /// Add the provided result and return whether or not the caller can halt
        /// processing.
        /// </summary>
        /// <param name="partialResult">The result to add</param>
        /// <param name="shard"></param>
        /// <returns>Whether or not the caller can halt processing</returns>
        public bool AddResult(IEnumerable<T> partialResult, IShard shard)
        {
            Preconditions.CheckNotNull(partialResult);

            result = result == null
                ? partialResult.Cast<T>()
                : result.Concat(partialResult.Cast<T>());
            return false;
        }

        public IEnumerable<T> CompileResults()
        {
            return exitOperation.Execute(result ?? Enumerable.Empty<T>());
        }
    }
}