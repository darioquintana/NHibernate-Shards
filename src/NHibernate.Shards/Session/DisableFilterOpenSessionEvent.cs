namespace NHibernate.Shards.Session
{
	/**
	 * OpenSessionEvent which disables specified filter.
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class DisableFilterOpenSessionEvent : IOpenSessionEvent
	{
	    private readonly string filterName;

        public DisableFilterOpenSessionEvent(string filterName)
        {
            this.filterName = filterName;
        }

		#region IOpenSessionEvent Members

		public void OnOpenSession(ISession session)
		{
		    session.DisableFilter(filterName);
		}

		#endregion
	}
}