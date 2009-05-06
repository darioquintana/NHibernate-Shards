using System;

namespace NHibernate.Shards.Criteria
{
	public class SetCacheRegionEvent : ICriteriaEvent
	{
		private readonly String cacheRegion;

		///<summary>Construct a CacheRegionEvent</summary>
		/// <param name="cacheRegion">the cache region we'll set on the {@link Criteria}
		/// when the event fires.</param>
		public SetCacheRegionEvent(String cacheRegion)
		{
			this.cacheRegion = cacheRegion;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetCacheRegion(cacheRegion);
		}
	}
}
