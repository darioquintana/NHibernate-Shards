using System.Collections.Generic;

namespace NHibernate.Shards.Criteria
{
	public interface ISubcriteriaRegistrar
	{
		void EstablishSubCriteria(ICriteria crit, ISubcriteriaFactory subcriteriaFactory,
		                          IDictionary<IShard, ICriteria> shardToCriteriaMap,
		                          IDictionary<IShard, IList<ICriteriaEvent>> shardToCriteriaEventListMap);
	}
}