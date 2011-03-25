namespace NHibernate.Shards.Query
{
	public class SetMaxResultsEvent : IQueryEvent
	{
		private readonly int maxResults;

		public SetMaxResultsEvent(int maxResults)
		{
			this.maxResults = maxResults;
		}

		public void OnEvent(IQuery query)
		{
			query.SetMaxResults(maxResults);
		}
	}
}
