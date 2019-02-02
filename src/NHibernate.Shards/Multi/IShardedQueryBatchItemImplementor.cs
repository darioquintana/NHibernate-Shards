namespace NHibernate.Shards.Multi
{
	using NHibernate.Multi;
	using NHibernate.Shards.Strategy.Exit;

	public interface IShardedQueryBatchItemImplementor : IQueryBatchItem
	{
		IExitOperationFactory ExitOperationFactory { get; }
		void EstablishFor(IShard shard, IQueryBatch queryBatch);
		void EstablishFor(IShard shard, string key, IQueryBatch queryBatch);
	}
}