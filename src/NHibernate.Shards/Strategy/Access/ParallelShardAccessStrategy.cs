using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Strategy.Access
{
    /// <summary>
    /// Invokes the given operation on the given shards in parallel.
    /// </summary>
    public class ParallelShardAccessStrategy : IShardAccessStrategy
    {
        private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);
        private static readonly IInternalLogger Log = LoggerProvider.LoggerFor(typeof(ParallelShardAccessStrategy));

        #region IShardAccessStrategy Members

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="shards"></param>
        /// <param name="operation"></param>
        /// <param name="exitStrategy"></param>
        /// <returns></returns>
        public T Apply<T>(IEnumerable<IShard> shards, IShardOperation<T> operation, IExitStrategy<T> exitStrategy)
        {
            object syncLock = new object();
            int activeCount = 0;                // Number of active operations
            bool cancelled = false;             // Has cancellation been requested of uncompleted operations?
            Exception exception = null;

            lock (syncLock)
            {
                foreach (var shard in shards)
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        // ReSharper disable AccessToModifiedClosure
                        var s = (IShard)state;
                        try
                        {
                            lock (syncLock)
                            {
                                // Prevent execution if parallel operation has already been cancelled.
                                if (cancelled)
                                {
                                    if (--activeCount <= 0) Monitor.Pulse(syncLock);
                                    return;
                                }
                            }

                            var result = operation.Execute(s);

                            lock (syncLock)
                            {
                                // Only add result if operation still has not been cancelled and result is not null.
                                if (!cancelled && !Equals(result, null))
                                {
                                    cancelled = exitStrategy.AddResult(result, s);
                                }

                                if (--activeCount <= 0) Monitor.Pulse(syncLock);
                            }
                        }
                        catch (Exception e)
                        {
                            lock (syncLock)
                            {
                                if (!cancelled)
                                {
                                    exception = e;
                                    cancelled = true;
                                }

                                if (--activeCount <= 0) Monitor.Pulse(syncLock);
                            }
                            Log.DebugFormat("Failed parallel operation '{0}' on shard '{1:X}'.",
                                operation.OperationName, s.ShardIds.First(), activeCount);
                        }
                        // ReSharper restore AccessToModifiedClosure
                    }, shard);
                    activeCount++;
                }

                DateTime now = DateTime.Now;
                DateTime deadline = now.Add(OperationTimeout);
                while (activeCount > 0)
                {
                    var timeout = deadline - now;
                    if (timeout <= TimeSpan.Zero || !Monitor.Wait(syncLock, timeout))
                    {
                        cancelled = true;

                        string message = string.Format(
                            CultureInfo.InvariantCulture,
                            "Parallel '{0}' operation did not complete in '{1}' seconds.",
                            operation.OperationName, OperationTimeout);
                        Log.Error(message);
                        throw new TimeoutException(message);
                    }

                    now = DateTime.Now;
                }
            }

            if (exception != null)
            {
                Log.Error(string.Format("Failed parallel '{0}' operation.", operation.OperationName), exception);
                throw exception;
            }

            Log.Debug(string.Format("Completed parallel '{0}' operation.", operation.OperationName), exception);
            return exitStrategy.CompileResults();
        }

        #endregion
    }
}