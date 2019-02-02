using System.Collections.Generic;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Strategy.Access
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface IShardAccessStrategy
	{
		void Apply(IEnumerable<IShard> shards, IShardOperation operation);
		Task ApplyAsync(IEnumerable<IShard> shards, IAsyncShardOperation operation, CancellationToken cancellationToken);

		T Apply<T>(IEnumerable<IShard> shards, IShardOperation<T> operation, IExitStrategy<T> exitStrategy);
		Task<T> ApplyAsync<T>(IEnumerable<IShard> shards, IAsyncShardOperation<T> operation, IExitStrategy<T> exitStrategy, CancellationToken cancellationToken);
	}
}