namespace NHibernate.Shards.Query
{
    internal class SetCacheableEvent : IQueryEvent
    {
        private readonly bool cacheable;

        public SetCacheableEvent(bool cacheable)
        {
            this.cacheable = cacheable;
        }

        public void OnEvent(IQuery query)
        {
            query.SetCacheable(cacheable);
        }
    }
}
