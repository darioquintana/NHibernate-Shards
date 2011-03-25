namespace NHibernate.Shards.Query
{
	public class SetTimeoutEvent : IQueryEvent
	{
		private readonly int timeout;

		public SetTimeoutEvent(int timeout)
		{
			this.timeout = timeout;
		}

		public void OnEvent(IQuery query)
		{
			query.SetTimeout(timeout);
		}

	}

}
