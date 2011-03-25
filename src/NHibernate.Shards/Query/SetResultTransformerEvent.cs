using NHibernate.Transform;

namespace NHibernate.Shards.Query
{
	public class SetResultTransformerEvent : IQueryEvent
	{
		private readonly IResultTransformer transformer;

		public SetResultTransformerEvent(IResultTransformer transformer)
		{
			this.transformer = transformer;
		}

		public void OnEvent(IQuery query)
		{
			query.SetResultTransformer(transformer);
		}

	}
}
