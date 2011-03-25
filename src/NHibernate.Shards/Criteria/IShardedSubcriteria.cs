namespace NHibernate.Shards.Criteria
{
	/**
	 * Interface describing a {@link CriteriaImpl.Subcriteria}
	 * that is shard-aware.  A ShardedSubcriteria must know how to provide
	 * a reference to its parent {@link ShardedCriteria}.
	 * @see CriteriaImpl.Subcriteria
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public interface IShardedSubcriteria : ICriteria
	{
		/**
		 * @return the owning ShardedCriteria
		 */
		IShardedCriteria ParentCriteria { get; }
	}
}