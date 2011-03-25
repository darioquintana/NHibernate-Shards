using NHibernate.Shards.Criteria;

namespace NHibernate.Shards
{
    public class ListShardOperation<T>:IShardOperation<T>
    {
        private IShardedCriteria shardedCriteria;

        public ListShardOperation(IShardedCriteria shardedCriteria)
        {
            this.shardedCriteria = shardedCriteria;
        }
        
        public T Execute(IShard shard)
        {
            shard.EstablishCriteria(shardedCriteria);
            return (T)shard.List(shardedCriteria.CriteriaId);
        }

        public string OperationName
        {
            get { return "list()"; }
        }
    }
}
