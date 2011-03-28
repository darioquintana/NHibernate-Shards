using System.Threading;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Exit
{
    internal interface IUniqueResult<T>
    {
        T Value { get; }
        IShard Shard { get; }
    }

    public class UniqueResultExitStrategy<T> : IExitStrategy<T>, IUniqueResult<T>
	{
        private int resultCount;
		private T firstResult;
        private IShard firstShard;

		/// <summary>
		/// Add the provided result and return whether or not the caller can halt
		/// processing.
		/// 
		/// Synchronized method guarantees that only the first thread to add a result  will have its result reflected.
		/// </summary>
		/// <param name="result">The result to add</param>
		/// <param name="shard"></param>
		/// <returns>Whether or not the caller can halt processing</returns>
		public bool AddResult(T result, IShard shard)
		{
            Preconditions.CheckNotNull(result);

            if (Interlocked.Increment(ref resultCount) == 1)
            {
                firstResult = result;
                firstShard = shard;
            }
			return true;
		}

        public T Value
        {
            get { return firstResult; }
        }
        
        public IShard Shard
        {
            get { return firstShard; }
        }

		public T CompileResults()
		{
			return firstResult;
		}
	}
}