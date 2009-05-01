using System;

namespace NHibernate.Shards.Criteria
{
    public class SetCacheRegionEvent : ICriteriaEvent
    {   
        private readonly String cacheRegion;

        /**
         * Construct a CacheRegionEvent
         *
         * @param cacheRegion the cache region we'll set on the {@link Criteria}
         * when the event fires.
         */
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
