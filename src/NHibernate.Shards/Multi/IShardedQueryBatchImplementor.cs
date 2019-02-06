namespace NHibernate.Shards.Multi
{
	using NHibernate.Multi;
	using NHibernate.Shards.Engine;

	public interface IShardedQueryBatchImplementor
	{
		IShardedSessionImplementor Session { get; }
		IQueryBatch EstablishFor(IShard shard);
	}
}