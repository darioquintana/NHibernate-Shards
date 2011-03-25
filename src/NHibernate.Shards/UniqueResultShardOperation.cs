using NHibernate.Shards.Criteria;

namespace NHibernate.Shards
{
	public class UniqueResultShardOperation<T> : IShardOperation<T>
	{
		private IShardedCriteria shardedCriteria;

		public UniqueResultShardOperation(IShardedCriteria shardedCriteria)
		{
			this.shardedCriteria = shardedCriteria;
		}

		public T Execute(IShard shard)
		{
			shard.EstablishCriteria(shardedCriteria);
			return (T) shard.UniqueResult(shardedCriteria.CriteriaId);
		}

		public string OperationName
		{
			get { return "uniqueResult()"; }
		}
	}
}