namespace NHibernate.Shards.Criteria
{

	/**
	 * Interface for events that can be laziliy applied to a
	 * {@link org.hibernate.Criteria}. Useful because we don't allocate a
	 * {@link org.hibernate.Criteria} until we actually need it, and programmers
	 * might be calling a variety of methods against the
	 * {@link org.hibernate.shards.criteria.ShardedCriteria}
	 * which need to be applied to the actual {@link org.hibernate.Criteria} once
	 * the actual {@link org.hibernate.Criteria} when it is allocated.
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public interface ICriteriaEvent
	{
		/**
		 * Apply the event
		 * @param crit the Criteria to apply the event to
		 */
		void OnEvent(ICriteria crit);
	}
}
