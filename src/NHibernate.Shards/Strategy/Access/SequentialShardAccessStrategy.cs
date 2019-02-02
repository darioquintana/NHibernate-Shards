using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Access
{
    public class SequentialShardAccessStrategy : IShardAccessStrategy
	{
	    private static readonly Logger Log = new Logger(typeof(SequentialShardAccessStrategy));

		#region IShardAccessStrategy Members

		public void Apply(IEnumerable<IShard> shards, IShardOperation operation)
		{
			foreach (var shard in GetNextOrderingOfShards(shards))
			{
				var shardOperation = operation.Prepare(shard);
				shardOperation();
			}
		}

		public T Apply<T>(IEnumerable<IShard> shards, IShardOperation<T> operation, IExitStrategy<T> exitStrategy)
		{
			foreach (var shard in GetNextOrderingOfShards(shards))
			{
				var shardOperation = operation.Prepare(shard);
				var result = shardOperation();
				if (result != null && exitStrategy.AddResult(result, shard))
				{
					Log.Debug("Short-circuiting operation {0} after execution against shard {1}", operation.OperationName, shard);
					break;
				}
			}
			return exitStrategy.CompileResults();
		}

		public async Task ApplyAsync(IEnumerable<IShard> shards, IAsyncShardOperation operation, CancellationToken cancellationToken)
		{
			foreach (var shard in GetNextOrderingOfShards(shards))
			{
				var shardOperation = operation.PrepareAsync(shard);
				await shardOperation(cancellationToken);
			}
		}

		public async Task<T> ApplyAsync<T>(IEnumerable<IShard> shards, IAsyncShardOperation<T> operation, IExitStrategy<T> exitStrategy, CancellationToken cancellationToken)
		{
			foreach (var shard in GetNextOrderingOfShards(shards))
			{
				var shardOperation = operation.PrepareAsync(shard);
				var result = await shardOperation(cancellationToken);
				if (result != null && exitStrategy.AddResult(result, shard))
				{
					Log.Debug("Short-circuiting operation {0} after execution against shard {1}", operation.OperationName, shard);
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
		/// distribute load).  Default implementation is to just iterate in the
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