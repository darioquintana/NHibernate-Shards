using NHibernate.Criterion;

namespace NHibernate.Shards.Criteria
{
	public class AddCriterionEvent : ICriteriaEvent
	{
		// the Criterion we're going to add when the event fires
		private readonly ICriterion criterion;

		public AddCriterionEvent(ICriterion criterion)
		{
			this.criterion = criterion;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.Add(criterion);
		}
	}
}
