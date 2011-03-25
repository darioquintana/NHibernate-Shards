namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the comment of a {@link Criteria} to be set lazily.
	 * @see Criteria#setComment(String)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class SetCommentEvent : ICriteriaEvent
	{
		// the comment that we'll set on the Criteria when the event fires
        private string comment;

		///<summary>Construct a SetCommentEvent</summary>
		/// <param name="comment">comment the comment we'll set on the {@link Criteria}
		/// when the event fires.</param>
		public SetCommentEvent(string comment)
		{
			this.comment = comment;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetComment(comment);
		}
	}
}
