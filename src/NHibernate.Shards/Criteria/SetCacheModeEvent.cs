

namespace NHibernate.Shards.Criteria
{
	public class SetCacheModeEvent : ICriteriaEvent
	{
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
