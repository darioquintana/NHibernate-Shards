using System.Collections.Generic;

namespace NHibernate.Shards.Criteria
{
	public interface ISubCriteriaFactory
	{
		///<summary>Create a sub {@link Criteria} with the given parent and events</summary>
		/// <param name="events">the events to apply</param>
		/// <param name="parent">parent the parent</param>
		/// <returns>return a criteria with the given parent and events</returns>
		ICriteria CreateSubcriteria(ICriteria parent, IList<ICriteriaEvent> events); //tenia iterable asi que le puse List
	}
}
