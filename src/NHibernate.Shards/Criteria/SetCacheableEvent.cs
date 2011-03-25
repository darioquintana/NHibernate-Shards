namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the cacheability of a {@link Criteria} to be set lazily.
	 * @see Criteria#setCacheable(boolean)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class SetCacheableEvent : ICriteriaEvent
	{
		// the value to which we're going to set the cacheability of the Criteria
		// when the event fires
		private readonly bool cacheable;

		///<summary>Construct a SetCacheableEvent</summary>
		/// <param name="cacheable">the value to which we'll set the cacheability when the event fires</param>
		public SetCacheableEvent(bool cacheable)
		{
			this.cacheable = cacheable;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetCacheable(cacheable);
		}
	}
}
