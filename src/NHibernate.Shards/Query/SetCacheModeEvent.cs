namespace NHibernate.Shards.Query
{
    class SetCacheModeEvent : IQueryEvent
    {
        private readonly CacheMode cacheMode;

        public SetCacheModeEvent(CacheMode cacheMode)
        {
            this.cacheMode = cacheMode;
        }

        public void OnEvent(IQuery query)
        {
            query.SetCacheMode(cacheMode);
        }
    }
}
