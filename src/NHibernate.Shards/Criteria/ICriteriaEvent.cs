namespace NHibernate.Shards.Criteria
{
	public interface ICriteriaEvent
	{
		void OnEvent(ICriteria crit);
	}
}
