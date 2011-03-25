using System.Runtime.CompilerServices;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Exit
{
	public class FirstNonNullResultExitStrategy<T> : IExitStrategy<T>
	{
		private T nonNullResult;
		private IShard shard;

		/// <summary>
		/// Add the provided result and return whether or not the caller can halt
		/// processing.
		/// 
		/// Synchronized method guarantees that only the first thread to add a result  will have its result reflected.
		/// </summary>
		/// <param name="result">The result to add</param>
		/// <param name="shard"></param>
		/// <returns>Whether or not the caller can halt processing</returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public bool AddResult(T result, IShard shard)
		{
			Preconditions.CheckNotNull(shard);
			if (result != null && nonNullResult == null)
			{
				nonNullResult = result;
				this.shard = shard;
				return true;
			}
			return false;
		}

		public T CompileResults(IExitOperationsCollector exitOperationsCollector)
		{
			return nonNullResult;
		}
	}
}