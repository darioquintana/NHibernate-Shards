using NHibernate.Transform;

namespace NHibernate.Shards.Criteria
{
	public class SetResultTransformerEvent : ICriteriaEvent
	{
		private readonly IResultTransformer resultTransformer;

		///<summary>Constructs a SetResultTransformerEvent</summary>
		/// <param name="resultTransformer">resultTransformer the resultTransformer we'll set on the {@link Criteria} when
		/// the event fires.</param>
		public SetResultTransformerEvent(IResultTransformer resultTransformer)
		{
			this.resultTransformer = resultTransformer;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetResultTransformer(resultTransformer);
		}
	}
}
