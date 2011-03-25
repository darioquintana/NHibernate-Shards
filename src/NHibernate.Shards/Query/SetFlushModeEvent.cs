namespace NHibernate.Shards.Query
{
	public class SetFlushModeEvent : IQueryEvent
	{
		private readonly FlushMode flushMode;

		public SetFlushModeEvent(FlushMode flushMode)
		{
			this.flushMode = flushMode;
		}

		public void OnEvent(IQuery query)
		{
			query.SetFlushMode(flushMode);
		}
	}
}
