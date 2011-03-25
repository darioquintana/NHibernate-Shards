namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the fetch size of a {@link Criteria} to be set lazily.
	 * @see Criteria#setFetchSize(int)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class SetFetchSizeEvent : ICriteriaEvent
    {
		// the fetchSize we'll set on the Criteria when the event fires.
		private readonly int fetchSize;

		///<summary>Construct a SetFetchSizeEvent</summary>
		/// <param name="fetchSize">the fetchSize that
		/// we'll set on the {@link Criteria} when the event fires.</param>
		public SetFetchSizeEvent(int fetchSize)
		{
			this.fetchSize = fetchSize;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetFetchSize(fetchSize);
		}
	}
}
