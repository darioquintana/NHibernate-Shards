
namespace NHibernate.Shards.Criteria
{
    public class SetCacheableEvent : ICriteriaEvent
    {
        private bool cacheable;

        /**
         * Construct a SetCacheableEvent
         *
         * @param cacheable the value to which we'll set the cacheability when the event
         * fires
         */
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
