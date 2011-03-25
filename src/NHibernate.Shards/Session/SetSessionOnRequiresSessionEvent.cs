namespace NHibernate.Shards.Session
{
	/**
	 * OpenSessionEvent which sets the provided Session on a RequiresSession.
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class SetSessionOnRequiresSessionEvent : IOpenSessionEvent
	{
	    private readonly IRequiresSession requiresSession;

        public SetSessionOnRequiresSessionEvent(IRequiresSession requiresSession)
        {
            this.requiresSession = requiresSession;    
        }
		#region IOpenSessionEvent Members

		public void OnOpenSession(ISession session)
		{
		    requiresSession.SetSession(session);
		}

		#endregion
	}
}