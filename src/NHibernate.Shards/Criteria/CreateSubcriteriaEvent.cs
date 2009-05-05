//namespace NHibernate.Shards.Criteria
//{
//    class CreateSubcriteriaEvent : ICriteriaEvent
//    {
//        private readonly ISubcriteriaFactory subcriteriaFactory;
//        private readonly IShardedSubcriteriaImpl.SubcriteriaRegistrar subcriteriaRegistrar;
        
//        public CreateSubcriteriaEvent(ISubcriteriaFactory subcriteriaFactory, ShardedSubcriteriaImpl.SubcriteriaRegistrar subcriteriaRegistrar) 
//        {
//          this.subcriteriaFactory = subcriteriaFactory;
//          this.subcriteriaRegistrar = subcriteriaRegistrar;
//        }

//        public void OnEvent(ICriteria crit)
//        {
//          subcriteriaRegistrar.establishSubcriteria(crit, subcriteriaFactory);
//        }
//    }
//}
