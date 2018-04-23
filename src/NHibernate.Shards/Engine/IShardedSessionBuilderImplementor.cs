namespace NHibernate.Shards.Engine
{
    public interface IShardedSessionBuilderImplementor 
    {
        IInterceptor SessionInterceptor { get; }
        ISession OpenSessionFor(IShard shard, IInterceptor interceptor);
    }
}