namespace NHibernate.Shards.Query
{
	public class SetFetchSizeEvent : IQueryEvent
	{
		private readonly int fetchSize;

		public SetFetchSizeEvent(int fetchSize)
		{
			this.fetchSize = fetchSize;
		}

		public void OnEvent(IQuery query)
		{
			query.SetFetchSize(fetchSize);
		}
	}
}
