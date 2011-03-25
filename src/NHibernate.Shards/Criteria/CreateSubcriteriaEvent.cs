using System.Collections.Generic;

namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows a Subcriteria to be lazily added to a Criteria.
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class CreateSubcriteriaEvent : ICriteriaEvent
	{
		private readonly ISubcriteriaFactory subcriteriaFactory;
		private readonly ISubcriteriaRegistrar subCriteriaRegistrar;
		private readonly IDictionary<IShard, ICriteria> shardToCriteriaMap;
		private readonly IDictionary<IShard, IList<ICriteriaEvent>> shardToCriteriaEventListMap;

		public CreateSubcriteriaEvent(ISubcriteriaFactory subCriteriaFactory, ISubcriteriaRegistrar subCriteriaRegistrar,
		                              IDictionary<IShard, ICriteria> shardToCriteriaMap,
		                              IDictionary<IShard, IList<ICriteriaEvent>> shardToCriteriaEventListMap)
		{
            this.subcriteriaFactory = subCriteriaFactory;
			this.subCriteriaRegistrar = subCriteriaRegistrar;
			this.shardToCriteriaMap = shardToCriteriaMap;
			this.shardToCriteriaEventListMap = shardToCriteriaEventListMap;
		}

		#region Implementation of ICriteriaEvent

		public void OnEvent(ICriteria crit)
		{
			subCriteriaRegistrar.EstablishSubCriteria(crit, subcriteriaFactory, shardToCriteriaMap, shardToCriteriaEventListMap);
		}

		#endregion
	}
}