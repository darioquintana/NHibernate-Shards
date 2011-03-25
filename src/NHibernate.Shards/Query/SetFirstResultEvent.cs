namespace NHibernate.Shards.Query
{
	public class SetFirstResultEvent : IQueryEvent
	{
		private readonly int firstResult;

		public SetFirstResultEvent(int firstResult)
		{
			this.firstResult = firstResult;
		}

		public void OnEvent(IQuery query)
		{
			query.SetFirstResult(firstResult);
		}

	}
}
