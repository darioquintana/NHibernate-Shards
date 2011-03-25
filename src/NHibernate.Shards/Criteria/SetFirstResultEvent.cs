namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the firstResult of a {@link Criteria} to be set lazily.
	 * @see Criteria#setFirstResult(int)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class SetFirstResultEvent : ICriteriaEvent
	{
		// the firstResult that we'll set on the Criteria when the event fires
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
