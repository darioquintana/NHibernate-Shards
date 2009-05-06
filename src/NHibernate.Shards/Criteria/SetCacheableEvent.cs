

namespace NHibernate.Shards.Criteria
{
	public class SetCacheableEvent : ICriteriaEvent
	{
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
