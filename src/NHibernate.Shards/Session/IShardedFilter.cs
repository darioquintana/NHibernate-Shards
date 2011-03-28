namespace NHibernate.Shards.Session
{
    public interface IShardedFilter: IFilter
    {
        IFilter EnableFor(ISession session);
        void Disable();
    }
}
