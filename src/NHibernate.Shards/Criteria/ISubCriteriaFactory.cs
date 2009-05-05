using System.Collections.Generic;

namespace NHibernate.Shards.Criteria
{
    public interface ISubCriteriaFactory
    {
        /**
       * Create a sub {@link Criteria} with the given parent and events
       *
       * @param parent the parent
       * @param events the events to apply
       * @return a criteria with the given parent and events
       */
        
        ICriteria CreateSubcriteria(ICriteria parent, IList<ICriteriaEvent> events);//tenia iterable asi que le puse List
    }
}
