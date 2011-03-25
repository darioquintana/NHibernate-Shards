namespace NHibernate.Shards.Session
{
	/**
	 * OpenSessionEvent which enables specified filter.
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class EnableFilterOpenSessionEvent : IOpenSessionEvent
	{
	    private string filterName;

        public EnableFilterOpenSessionEvent(string filterName)
        {
            this.filterName = filterName;
        }

		public void OnOpenSession(ISession session)
		{
		    session.EnableFilter(filterName);
		}
	}
}
