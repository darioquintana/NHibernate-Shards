using System.Collections.Generic;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Strategy.Access
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface IShardAccessStrategy
	{
		T Apply<T>(IEnumerable<IShard> shards, IShardOperation<T> operation, IExitStrategy<T> exitStrategy);

		Task<T> ApplyAsync<T>(IEnumerable<IShard> shards, IAsyncShardOperation<T> operation, IExitStrategy<T> exitStrategy,
			CancellationToken cancellationToken);
	}
}