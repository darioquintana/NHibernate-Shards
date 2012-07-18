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
        	return new ParallelOperation<T>(shards, operation, exitStrategy).Complete();
        }

        #endregion

		#region Inner classes

		private class ParallelOperation<T>
		{
			private readonly IShardOperation<T> _operation;
			private readonly IExitStrategy<T> _exitStrategy;

			private Exception _exception;
			private bool _isCancelled;
			private int _activeCount;

			public ParallelOperation(IEnumerable<IShard> shards, IShardOperation<T> operation, IExitStrategy<T> exitStrategy)
			{
				_operation = operation;
				_exitStrategy = exitStrategy;

				lock (this)
				{
					foreach (var shard in shards)
					{
						ThreadPool.QueueUserWorkItem(ExecuteForShard, shard);
						_activeCount++;
					}
				}
			}

			public T Complete()
			{
				lock (this)
				{
					DateTime now = DateTime.Now;
					DateTime deadline = now.Add(OperationTimeout);
					while (_activeCount > 0)
					{
						var timeout = deadline - now;
						if (timeout <= TimeSpan.Zero || !Monitor.Wait(this, timeout))
						{
							_isCancelled = true;

							string message = string.Format(
								CultureInfo.InvariantCulture,
								"Parallel '{0}' operation did not complete in '{1}' seconds.",
								_operation.OperationName, OperationTimeout);
							Log.Error(message);
							throw new TimeoutException(message);
						}

						now = DateTime.Now;
					}
				}

				if (_exception != null)
				{
					var message = string.Format("Failed parallel '{0}' operation.", _operation.OperationName);
					Log.Error(message, _exception);
					throw new HibernateException(message, _exception);
				}

				Log.Debug(string.Format("Completed parallel '{0}' operation.", _operation.OperationName), _exception);
				return _exitStrategy.CompileResults();
			}

			private void ExecuteForShard(object state)
			{
				var s = (IShard)state;
				try
				{
					Func<T> shardOperation;

					// Perform thread-safe preparation of the operation for a single shard.
					lock (this)
					{
						// Prevent execution if parallel operation has already been cancelled.
						if (_isCancelled)
						{
							if (--_activeCount <= 0) Monitor.Pulse(this);
							return;
						}

						shardOperation = _operation.Prepare(s);
					}

					// Perform operation execution on multiple shards in parallel.
					var result = shardOperation();

					// Perform thread-safe aggregation of operation results.
					lock (this)
					{
						// Only add result if operation still has not been cancelled and result is not null.
						if (!_isCancelled && !Equals(result, null))
						{
							_isCancelled = _exitStrategy.AddResult(result, s);
						}

						if (--_activeCount <= 0) Monitor.Pulse(this);
					}
				}
				catch (Exception e)
				{
					lock (this)
					{
						if (!_isCancelled)
						{
							_exception = e;
							_isCancelled = true;
						}

						if (--_activeCount <= 0) Monitor.Pulse(this);
					}
					Log.DebugFormat("Failed parallel operation '{0}' on shard '{1:X}'.",
						_operation.OperationName, s.ShardIds.First());
				}
			}
		}

		#endregion
	}
}