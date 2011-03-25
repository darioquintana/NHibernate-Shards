namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the {@link CacheMode} of a {@link Criteria} to be set lazily.
	 * @see Criteria#setCacheMode(CacheMode)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class SetCacheModeEvent : ICriteriaEvent
	{
		// the CacheMode that we'll set on the Criteria when the event fires
		private readonly CacheMode cacheMode;

		///<summary>Construct a CacheModeEvent</summary>
		/// <param name="cacheMode">cacheMode the {@link CacheMode} we'll set on the {@link Criteria} 
		/// when the event fires.</param>
		public SetCacheModeEvent(CacheMode cacheMode)
		{
			this.cacheMode = cacheMode;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetCacheMode(cacheMode);
		}
	}
}
