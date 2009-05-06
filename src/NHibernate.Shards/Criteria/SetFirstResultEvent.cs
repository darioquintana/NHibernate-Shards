

namespace NHibernate.Shards.Criteria
{
	public class SetFirstResultEvent : ICriteriaEvent
	{
		private readonly int firstResult;

		///<summary>Construct a SetFirstResultEvent</summary>
		/// <param name="firstResult">the firstResult that
		/// we'll set on the {@link Criteria} when the event fires.</param>
		public SetFirstResultEvent(int firstResult)
		{
			this.firstResult = firstResult;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetFirstResult(firstResult);
		}
	}
}
