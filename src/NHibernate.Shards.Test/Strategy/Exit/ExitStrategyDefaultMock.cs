using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Test.Strategy.Exit
{
	public class ExitStrategyDefaultMock<T> : IExitStrategy<T>
	{
		/// <summary>
		/// Add the provided result and return whether or not the caller can halt
		/// processing.
		/// </summary>
		/// <param name="result">The result to add</param>
		/// <param name="shard"></param>
		/// <returns>Whether or not the caller can halt processing</returns>
		public bool AddResult(T result, IShard shard)
		{
			throw new System.NotSupportedException();
		}

		public T CompileResults()
		{
			throw new System.NotSupportedException();
		}
	}
}