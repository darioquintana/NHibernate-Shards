using System.Collections.Generic;

namespace NHibernate.Shards.Criteria
{
	/**
	 * Interface describing an object tha knows how to create {@link Criteria}.
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public interface ISubcriteriaFactory
	{
		/**
		 * Create a sub {@link Criteria} with the given parent and events
		 *
		 * @param parent the parent
		 * @param events the events to apply
		 * @return a criteria with the given parent and events
		 */
		ICriteria CreateSubcriteria(ICriteria parent, IEnumerable<ICriteriaEvent> events);
	}
}