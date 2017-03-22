using System.Collections.Generic;

namespace NHibernate.Shards.Strategy.Exit
{
    using NHibernate.Shards.Util;

    internal interface IUniqueResult<T>
	{
		T Value { get; }
		IShard Shard { get; }
	}

	public class UniqueResultExitStrategy<T> : IExitStrategy<T>, IUniqueResult<T>
	{
	    private readonly AggregationFunc aggregation;
	    private readonly List<T> results = new List<T>();
		private IShard firstShard;

	    public UniqueResultExitStrategy(AggregationFunc aggregation)
	    {
	        this.aggregation = aggregation;
	    }

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
		    this.results.Add(result);
            this.firstShard = this.results.Count == 1
                ? shard
                : null;
			return false;
		}

		public T Value
		{
		    get
		    {
                if (this.results.Count <= 0) return default(T);
                if (this.results.Count > 1) throw new NonUniqueResultException(this.results.Count);
                return this.results[0];
            }
        }
		
		public IShard Shard
		{
			get { return this.firstShard; }
		}

		public T CompileResults()
		{
		    if (this.aggregation != null)
		    {
		        var aggregationResult = (T)this.aggregation(this.results);
		        this.results.Clear();
		        this.results.Add(aggregationResult);
		    }
		    return this.Value;
		}
    }
}