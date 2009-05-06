using System;

namespace NHibernate.Shards.Criteria
{
	public class SetFetchModeEvent : ICriteriaEvent
	{
		private readonly String associationPath;

		// the FetchMode that will be set on the Criteria
		private readonly FetchMode mode;

		///<summary>Construct a SetFetchModeEvent</summary>
		/// <param name="associationPath">the association path of the fetch mode
		/// we'll set on the {@link Criteria} when the event fires.</param>
		/// <param name="mode">the mode we'll set on the {@link Criteria} when the event fires.</param>
		public SetFetchModeEvent(String associationPath, FetchMode mode)
		{
			this.associationPath = associationPath;
			this.mode = mode;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetFetchMode(associationPath, mode);
		}
	}
}
