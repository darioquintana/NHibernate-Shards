namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the cache region of a {@link Criteria} to be set lazily.
	 * @see Criteria#setCacheRegion(String)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class SetCacheRegionEvent : ICriteriaEvent
	{
		// the cache region that we'll set on the Criteria when the event fires
        private readonly string cacheRegion;

		///<summary>Construct a CacheRegionEvent</summary>
		/// <param name="cacheRegion">the cache region we'll set on the {@link Criteria}
		/// when the event fires.</param>
		public SetCacheRegionEvent(string cacheRegion)
		{
			this.cacheRegion = cacheRegion;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetCacheRegion(cacheRegion);
		}
	}
}
