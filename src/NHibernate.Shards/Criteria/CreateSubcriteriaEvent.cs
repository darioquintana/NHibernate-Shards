namespace NHibernate.Shards.Criteria
{
	internal class CreateSubcriteriaEvent : ICriteriaEvent
	{
		private readonly ISubCriteriaFactory subcriteriaFactory;
		private readonly ShardedSubcriteriaImpl.ISubcriteriaRegistrar subcriteriaRegistrar;

		public ISubCriteriaFactory SubcriteriaFactory
		{
			get { return subcriteriaFactory; }
		}

		public CreateSubcriteriaEvent(ISubCriteriaFactory subcriteriaFactory,
		                              ShardedSubcriteriaImpl.ISubcriteriaRegistrar subcriteriaRegistrar)
		{
			this.subcriteriaFactory = subcriteriaFactory;
			this.subcriteriaRegistrar = subcriteriaRegistrar;
		}

		public void OnEvent(ICriteria crit)
		{
			subcriteriaRegistrar.EstablishSubcriteria(crit, subcriteriaFactory);
		}
	}
}
