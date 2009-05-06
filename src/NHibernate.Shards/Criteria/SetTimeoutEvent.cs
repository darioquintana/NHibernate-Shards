

namespace NHibernate.Shards.Criteria
{
	public class SetTimeoutEvent : ICriteriaEvent
	{
		private readonly int timeout;

		///<summary>Constructs a SetTimeoutEvent</summary>
		/// <param name="timeout">timeout the timeout we'll set on the {@link Criteria} when the
		/// event fires.</param>
		public SetTimeoutEvent(int timeout)
		{
			this.timeout = timeout;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetTimeout(timeout);
		}
	}
}
