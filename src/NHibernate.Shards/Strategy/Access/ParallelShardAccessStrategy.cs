using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Strategy.Access
{
	using System.Threading.Tasks;

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

		public Task<T> ApplyAsync<T>(IEnumerable<IShard> shards, IAsyncShardOperation<T> operation, IExitStrategy<T> exitStrategy, CancellationToken cancellationToken)
		{
			return new ParallelAsyncOperation<T>(shards, operation, exitStrategy, cancellationToken).CompleteAsync();
		}

		private static TimeoutException CreateAndLogTimeoutException(string operationName)
		{
			string message = string.Format(
				CultureInfo.InvariantCulture,
				"Parallel '{0}' operation did not complete in '{1}' seconds.",
				operationName, OperationTimeout);
			Log.Error(message);
			return new TimeoutException(message);
		}

		private static HibernateException WrapAndLogShardException(string operationName, Exception exception)
		{
			var message = string.Format("Failed parallel '{0}' operation.", operationName);
			Log.Error(message, exception);
			return new HibernateException(message, exception);
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
							throw CreateAndLogTimeoutException(this._operation.OperationName);
						}

						now = DateTime.Now;
					}
				}

				if (_exception != null)
				{
					throw WrapAndLogShardException(_operation.OperationName, _exception);
				}

				Log.Debug(string.Format("Completed parallel '{0}' operation.", _operation.OperationName));
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

		private class ParallelAsyncOperation<T>
		{
			private readonly IAsyncShardOperation<T> _operation;
			private readonly IExitStrategy<T> _exitStrategy;
			private readonly CancellationTokenSource _completedCts = new CancellationTokenSource();
			private readonly Task<T> _task;

			public ParallelAsyncOperation(IEnumerable<IShard> shards, IAsyncShardOperation<T> operation, IExitStrategy<T> exitStrategy, CancellationToken cancellationToken)
			{
				_operation = operation;
				_exitStrategy = exitStrategy;
				_completedCts = new CancellationTokenSource();

				var timeoutCts = new CancellationTokenSource(OperationTimeout);
				var linkedCts = cancellationToken.CanBeCanceled
					? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token, _completedCts.Token)
					: CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _completedCts.Token);

				_task = ExecuteForShardsAsync(shards, linkedCts.Token).ContinueWith(t =>
				{
					if (t.IsCanceled && !_completedCts.IsCancellationRequested)
					{
						if (timeoutCts.IsCancellationRequested) throw CreateAndLogTimeoutException(operation.OperationName);
						linkedCts.Token.ThrowIfCancellationRequested();
					}
					if (t.Exception != null)
					{
						throw WrapAndLogShardException(operation.OperationName, t.Exception.GetBaseException());
					}
					return exitStrategy.CompileResults();
				});
			}

			public Task<T> CompleteAsync()
			{
				return _task;
			}

			private Task ExecuteForShardsAsync(IEnumerable<IShard> shards, CancellationToken cancellationToken)
			{
				var shardTasks = new List<Task>();
				foreach (var shard in shards)
				{
					if (cancellationToken.IsCancellationRequested) break;
					shardTasks.Add(ExecuteForShardAsync(shard, cancellationToken));
				}

				return Task.WhenAll(shardTasks);
			}

			private Task ExecuteForShardAsync(IShard shard, CancellationToken cancellationToken)
			{
				var shardOperation = _operation.PrepareAsync(shard);
				return shardOperation(cancellationToken).ContinueWith(t =>
				{
					if (!t.IsCanceled && _exitStrategy.AddResult(t.Result, shard))
					{
						_completedCts.Cancel();
					}
				});
			}
		}

		#endregion
	}
}