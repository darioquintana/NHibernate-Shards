namespace NHibernate.Shards
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Represents a single operation that can be performed on a shard.
	/// </summary>
	public interface IAsyncShardOperation
	{
		/// <summary>
		/// Creates thread-safe delegate that will perform the operation on a given shard.
		/// </summary>
		/// <param name="shard">The shard to execute against</param>
		/// <returns>Thread-safe delegate that will perform the operation on <paramref name="shard"/>.</returns>
		/// <remarks>The delegate returned by this method may be executed in parallel for multiple 
		/// shards. The delegate MUST therefore not perform any operations on state that is shared 
		/// across shards. This implies that the establishing of shard-specific sessions, queries and/or 
		/// criterias must be done before the delegate is returned by this method.</remarks>
		Func<CancellationToken, Task> PrepareAsync(IShard shard);

		/// <summary>
		/// The name of the operation (useful for logging and debugging)
		/// </summary>
		string OperationName { get; }
	}

	/// <summary>
	/// Represents a single operation that can be performed on a shard.
	/// </summary>
	/// <typeparam name="T">Operation result type</typeparam>
	public interface IAsyncShardOperation<T>
	{
		/// <summary>
		/// Creates thread-safe delegate that will perform the operation on a given shard.
		/// </summary>
		/// <param name="shard">The shard to execute against</param>
		/// <returns>Thread-safe delegate that will perform the operation on <paramref name="shard"/>.</returns>
		/// <remarks>The delegate returned by this method may be executed in parallel for multiple 
		/// shards. The delegate MUST therefore not perform any operations on state that is shared 
		/// across shards. This implies that the establishing of shard-specific sessions, queries and/or 
		/// criterias must be done before the delegate is returned by this method.</remarks>
		Func<CancellationToken, Task<T>> PrepareAsync(IShard shard);

		/// <summary>
		/// The name of the operation (useful for logging and debugging)
		/// </summary>
		string OperationName { get; }
	}
}