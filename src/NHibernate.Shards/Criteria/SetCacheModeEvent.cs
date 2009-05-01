
namespace NHibernate.Shards.Criteria
{
    public class SetCacheModeEvent : ICriteriaEvent
    {
        private readonly CacheMode cacheMode;

        /**
         * Construct a CacheModeEvent
         *
         * @param cacheMode the {@link CacheMode} we'll set on the {@link Criteria}
         * when the event fires.
         */
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
