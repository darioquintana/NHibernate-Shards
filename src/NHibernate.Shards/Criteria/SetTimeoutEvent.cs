namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the timeout of a {@link Criteria} to be set lazily.
	 * @see Criteria#setTimeout(int)   
	 *
	 * @author maxr@google.com (Max Ross)
	 */
    class SetTimeoutEvent:ICriteriaEvent
    {
		// the timeout we'll set on the Criteria when the event fires.
        private readonly int timeout;

		/**
		 * Constructs a SetTimeoutEvent
		 *
		 * @param timeout the timeout we'll set on the {@link Criteria} when the
		 * event fires.
		 */
        public SetTimeoutEvent(int timeout)
        {
            this.timeout = timeout;
        }

        #region Implementation of ICriteriaEvent

        public void OnEvent(ICriteria crit)
        {
            crit.SetTimeout(timeout);
        }

        #endregion
    }
}
