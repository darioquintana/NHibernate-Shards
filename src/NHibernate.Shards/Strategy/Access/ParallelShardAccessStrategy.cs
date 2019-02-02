using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Access
{
	/// <summary>
	/// Invokes the given operation on the given shards in parallel.
	/// </summary>
	public class ParallelShardAccessStrategy : IShardAccessStrategy
	{
		private static readonly TimeSpan OperationTimeoutInSeconds = TimeSpan.FromSeconds(30);
	    private static readonly Logger Log = new Logger(typeof(ParallelShardAccessStrategy));

		#region IShardAccessStrategy Members

		/// <inheritdoc />
		public void Apply(IEnumerable<IShard> shards, IShardOperation operation)
		{
			new ParallelOperation(shards, operation).Complete();
		}

		/// <inheritdoc />
		public T Apply<T>(IEnumerable<IShard> shards, IShardOperation<T> operation, IExitStrategy<T> exitStrategy)
		{
			return new ParallelOperation<T>(shards, operation, exitStrategy).Complete();
		}

		/// <inheritdoc />
		public Task ApplyAsync(IEnumerable<IShard> shards, IAsyncShardOperation operation, CancellationToken cancellationToken)
		{
			return new ParallelAsyncOperation(shards, operation, cancellationToken).CompleteAsync();
		}

		/// <inheritdoc />
		public Task<T> ApplyAsync<T>(IEnumerable<IShard> shards, IAsyncShardOperation<T> operation, IExitStrategy<T> exitStrategy, CancellationToken cancellationToken)
		{
			return new ParallelAsyncOperation<T>(shards, operation, exitStrategy, cancellationToken).CompleteAsync();
		}

		private static TimeoutException CreateAndLogTimeoutException(string operationName)
		{
			string message = $"Parallel '{operationName}' operation did not complete in '{OperationTimeoutInSeconds}' seconds.";
			Log.Error(message);
			return new TimeoutException(message);
		}

		private static HibernateException WrapAndLogShardException(string operationName, Exception exception)
		{
			var message = $"Failed parallel '{operationName}' operation.";
			Log.Error(exception, message);
			return new HibernateException(message, exception);
		}

		#endregion

		#region Inner classes

		private class ParallelOperation
		{
			private readonly IShardOperation operation;

			private Exception exception;
			private bool isCancelled;
			private int activeCount;

			public ParallelOperation(IEnumerable<IShard> shards, IShardOperation operation)
			{
				this.operation = operation;

				lock (this)
				{
					foreach (var shard in shards)
					{
						ThreadPool.QueueUserWorkItem(ExecuteForShard, shard);
						this.activeCount++;
					}
				}
			}

			public void Complete()
			{
				lock (this)
				{
					DateTime now = DateTime.Now;
					DateTime deadline = now.Add(OperationTimeoutInSeconds);
					while (this.activeCount > 0)
					{
						var timeout = deadline - now;
						if (timeout <= TimeSpan.Zero || !Monitor.Wait(this, timeout))
						{
							this.isCancelled = true;
							throw CreateAndLogTimeoutException(this.operation.OperationName);
						}

						now = DateTime.Now;
					}
				}

				if (this.exception != null)
				{
					throw WrapAndLogShardException(this.operation.OperationName, this.exception);
				}

				Log.Debug($"Completed parallel '{this.operation.OperationName}' operation.");
			}

			private void ExecuteForShard(object state)
			{
				var s = (IShard)state;
				try
				{
					System.Action shardOperation;

					// Perform thread-safe preparation of the operation for a single shard.
					lock (this)
					{
						// Prevent execution if parallel operation has already been cancelled.
						if (this.isCancelled)
						{
							if (--this.activeCount <= 0) Monitor.Pulse(this);
							return;
						}

						shardOperation = this.operation.Prepare(s);
					}

					// Perform operation execution on multiple shards in parallel.
					shardOperation();

					// Perform thread-safe aggregation of operation results.
					lock (this)
					{
						if (--this.activeCount <= 0) Monitor.Pulse(this);
					}
				}
				catch (Exception e)
				{
					lock (this)
					{
						if (!this.isCancelled)
						{
							this.exception = e;
							this.isCancelled = true;
						}

						if (--this.activeCount <= 0) Monitor.Pulse(this);
					}
					Log.Debug("Failed parallel operation '{0}' on shard '{1:X}'.",
						this.operation.OperationName, s.ShardIds.First());
				}
			}
		}

		private class ParallelOperation<T>
		{
			private readonly IShardOperation<T> operation;
			private readonly IExitStrategy<T> exitStrategy;

			private Exception exception;
			private bool isCancelled;
			private int activeCount;

			public ParallelOperation(IEnumerable<IShard> shards, IShardOperation<T> operation, IExitStrategy<T> exitStrategy)
			{
				this.operation = operation;
				this.exitStrategy = exitStrategy;

				lock (this)
				{
					foreach (var shard in shards)
					{
						ThreadPool.QueueUserWorkItem(ExecuteForShard, shard);
						this.activeCount++;
					}
				}
			}

			public T Complete()
			{
				lock (this)
				{
					DateTime now = DateTime.Now;
					DateTime deadline = now.Add(OperationTimeoutInSeconds);
					while (this.activeCount > 0)
					{
						var timeout = deadline - now;
						if (timeout <= TimeSpan.Zero || !Monitor.Wait(this, timeout))
						{
							this.isCancelled = true;
							throw CreateAndLogTimeoutException(this.operation.OperationName);
						}

						now = DateTime.Now;
					}
				}

				if (this.exception != null)
				{
					throw WrapAndLogShardException(this.operation.OperationName, this.exception);
				}

				Log.Debug($"Completed parallel '{this.operation.OperationName}' operation.");
				return this.exitStrategy.CompileResults();
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
						if (this.isCancelled)
						{
							if (--this.activeCount <= 0) Monitor.Pulse(this);
							return;
						}

						shardOperation = this.operation.Prepare(s);
					}

					// Perform operation execution on multiple shards in parallel.
					var result = shardOperation();

					// Perform thread-safe aggregation of operation results.
					lock (this)
					{
						// Only add result if operation still has not been cancelled and result is not null.
						if (!this.isCancelled && !Equals(result, null))
						{
							this.isCancelled = this.exitStrategy.AddResult(result, s);
						}

						if (--this.activeCount <= 0) Monitor.Pulse(this);
					}
				}
				catch (Exception e)
				{
					lock (this)
					{
						if (!this.isCancelled)
						{
							this.exception = e;
							this.isCancelled = true;
						}

						if (--this.activeCount <= 0) Monitor.Pulse(this);
					}
					Log.Debug("Failed parallel operation '{0}' on shard '{1:X}'.",
						this.operation.OperationName, s.ShardIds.First());
				}
			}
		}

		private class ParallelAsyncOperation
		{
			private readonly IAsyncShardOperation operation;
			private readonly Task task;

			public ParallelAsyncOperation(IEnumerable<IShard> shards, IAsyncShardOperation operation, CancellationToken cancellationToken)
			{
				this.operation = operation;

				var timeoutCts = new CancellationTokenSource(OperationTimeoutInSeconds);
				var linkedCts = cancellationToken.CanBeCanceled
					? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
					: CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token);

				this.task = ExecuteForShardsAsync(shards, linkedCts.Token).ContinueWith(t =>
				{
					if (t.IsCanceled)
					{
						if (timeoutCts.IsCancellationRequested) throw CreateAndLogTimeoutException(operation.OperationName);
						linkedCts.Token.ThrowIfCancellationRequested();
					}
					if (t.Exception != null)
					{
						throw WrapAndLogShardException(operation.OperationName, t.Exception.GetBaseException());
					}
				});
			}

			public Task CompleteAsync()
			{
				return this.task;
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
				var shardOperation = this.operation.PrepareAsync(shard);
				return shardOperation(cancellationToken);
			}
		}

		private class ParallelAsyncOperation<T>
		{
			private readonly IAsyncShardOperation<T> operation;
			private readonly IExitStrategy<T> exitStrategy;
			private readonly Task<T> task;

			/// <summary>
			/// Signals that exit strategy did shortcut evaluation of shard results 
			/// </summary>
			private readonly CancellationTokenSource completedCts = new CancellationTokenSource();

			public ParallelAsyncOperation(IEnumerable<IShard> shards, IAsyncShardOperation<T> operation, IExitStrategy<T> exitStrategy, CancellationToken cancellationToken)
			{
				this.operation = operation;
				this.exitStrategy = exitStrategy;

				var timeoutCts = new CancellationTokenSource(OperationTimeoutInSeconds);
				var linkedCts = cancellationToken.CanBeCanceled
					? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token, this.completedCts.Token)
					: CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, this.completedCts.Token);

				this.task = ExecuteForShardsAsync(shards, linkedCts.Token).ContinueWith(t =>
				{
					if (t.IsCanceled && !this.completedCts.IsCancellationRequested)
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
				return this.task;
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
				var shardOperation = this.operation.PrepareAsync(shard);
				return shardOperation(cancellationToken)
					.ContinueWith(t =>
						{
							if (this.exitStrategy.AddResult(t.Result, shard)) this.completedCts.Cancel();
						}, 
						TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
			}
		}

		#endregion
	}
}