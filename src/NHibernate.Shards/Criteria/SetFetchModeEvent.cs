﻿namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the {@link FetchMode} of a {@link Criteria} to be set lazily.
	 * @see Criteria#setFetchMode(String, FetchMode)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class SetFetchModeEvent : ICriteriaEvent
	{
		// the association path that will be set on the Criteria
		private readonly string associationPath;

		// the FetchMode that will be set on the Criteria
		private readonly FetchMode mode;

		///<summary>Construct a SetFetchModeEvent</summary>
		/// <param name="associationPath">the association path of the fetch mode
		/// we'll set on the {@link Criteria} when the event fires.</param>
		/// <param name="mode">the mode we'll set on the {@link Criteria} when the event fires.</param>
		public SetFetchModeEvent(string associationPath, FetchMode mode)
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
