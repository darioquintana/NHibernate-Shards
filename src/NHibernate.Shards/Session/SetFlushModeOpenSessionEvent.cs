namespace NHibernate.Shards.Session
{
	public class SetFlushModeOpenSessionEvent : IOpenSessionEvent
	{
		private readonly FlushMode flushMode;

		public SetFlushModeOpenSessionEvent(FlushMode flushMode)
		{
			this.flushMode = flushMode;
		}

		#region IOpenSessionEvent Members

		public void OnOpenSession(ISession session)
		{
			session.FlushMode = flushMode;
		}

		#endregion
	}
}