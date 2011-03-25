namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the {@link FlushMode} of a {@link Criteria} to be set lazily.
	 * @see Criteria#setFlushMode(FlushMode)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class SetFlushModeEvent : ICriteriaEvent
	{
		// the flushMode we'll set on the Critiera when the event fires
		private readonly FlushMode flushMode;

		///<summary>Construct a SetFlushModeEvent</summary>
		/// <param name="flushMode">the flushMode that
		/// we'll set on the {@link Criteria} when the event fires.</param>
		public SetFlushModeEvent(FlushMode flushMode)
		{
			this.flushMode = flushMode;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetFlushMode(flushMode);
		}
	}
}
