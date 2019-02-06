namespace NHibernate.Shards.Criteria
{
	public interface IShardedQueryOverImplementor
	{
		IShardedCriteria ShardedRootCriteria { get; }
	}
}