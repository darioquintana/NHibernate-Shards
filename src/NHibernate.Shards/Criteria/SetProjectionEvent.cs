using NHibernate.Criterion;

namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the {@link Projection} of a {@link Criteria} to be set lazily.
	 * @see Criteria#setProjection(Projection)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class SetProjectionEvent : ICriteriaEvent
	{
		// the Projection we'll set on the Critiera when the event fires
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
