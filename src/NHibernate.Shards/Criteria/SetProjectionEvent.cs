using NHibernate.Criterion;

namespace NHibernate.Shards.Criteria
{
	public class SetProjectionEvent : ICriteriaEvent
	{
		private readonly IProjection projection;

		///<summary>Constructs a SetProjectionEvent</summary>
		/// <param name="projection">the projection we'll set on the {@link Criteria} when the
		/// event fires.</param>
		public SetProjectionEvent(IProjection projection)
		{
			this.projection = projection;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetProjection(projection);
		}
	}
}
