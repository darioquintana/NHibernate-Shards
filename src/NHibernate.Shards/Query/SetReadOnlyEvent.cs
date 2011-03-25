namespace NHibernate.Shards.Query
{
	public class SetReadOnlyEvent : IQueryEvent
	{
		private readonly bool readOnly;

		public SetReadOnlyEvent(bool readOnly)
		{
			this.readOnly = readOnly;
		}

		public void OnEvent(IQuery query)
		{
			query.SetReadOnly(readOnly);
		}
	}
}
