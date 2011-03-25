using System.Collections.Generic;

namespace NHibernate.Shards.Criteria
{
    class SubcriteriaRegistrar:ISubcriteriaRegistrar
    {
        private readonly IShard shard;

        public SubcriteriaRegistrar(IShard shard)
        {
            this.shard = shard;
        }

        public void EstablishSubCriteria(ICriteria parentCriteria, ISubcriteriaFactory subcriteriaFactory, IDictionary<IShard, ICriteria> shardToCriteriaMap, IDictionary<IShard, IList<ICriteriaEvent>> shardToCriteriaEventListMap)
        {
            IList<ICriteriaEvent> criteriaEvents = shardToCriteriaEventListMap[shard];
			// create the subcrit with the proper list of events
            ICriteria newCrit = subcriteriaFactory.CreateSubcriteria(parentCriteria, criteriaEvents);
			// clear the list of events
            criteriaEvents.Clear();
			// add it to our map
            shardToCriteriaMap[shard] = newCrit;
        }
    }
}
