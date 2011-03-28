using System.Collections.Generic;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Strategy.Access
{
    public class SequentialShardAccessStrategy : IShardAccessStrategy
    {
        private readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(SequentialShardAccessStrategy));

        #region IShardAccessStrategy Members

        public T Apply<T>(IEnumerable<IShard> shards, IShardOperation<T> operation, IExitStrategy<T> exitStrategy)
        {
            foreach (IShard shard in GetNextOrderingOfShards(shards))
            {
                var result = operation.Execute(shard);
                if (result != null && exitStrategy.AddResult(result, shard))
                {
                    log.DebugFormat("Short-circuiting operation {0} after execution against shard {1}",
                                  operation.OperationName, shard);
                    break;
                }
            }
            return exitStrategy.CompileResults();
        }

        #endregion

        /// <summary>
        /// Override this method if you want to control the order in which the
        /// shards are operated on (this comes in handy when paired with exit
        /// strategies that allow early exit because it allows you to evenly
        /// distribute load).  Deafult implementation is to just iterate in the
        /// same order every time.
        /// </summary>
        /// <param name="shards">The shards we might want to reorder</param>
        /// <returns>Reordered view of the shards.</returns>
        protected virtual IEnumerable<IShard> GetNextOrderingOfShards(IEnumerable<IShard> shards)
        {
            return shards;
        }
    }
}