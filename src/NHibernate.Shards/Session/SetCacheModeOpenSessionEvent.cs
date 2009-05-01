namespace NHibernate.Shards.Session
{
	/// <summary>
	/// OpenSessionEvent which sets the CacheMode.
	/// </summary>
	public class SetCacheModeOpenSessionEvent : IOpenSessionEvent
	{
		private readonly CacheMode cacheMode;

		public SetCacheModeOpenSessionEvent(CacheMode cacheMode)
		{
			this.cacheMode = cacheMode;
		}

		#region IOpenSessionEvent Members

		public void OnOpenSession(ISession session)
		{
			session.CacheMode = cacheMode;
		}

		#endregion
	}
}