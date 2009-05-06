using System;

namespace NHibernate.Shards.Criteria
{
	public class SetCommentEvent : ICriteriaEvent
	{
		private readonly String comment;

		///<summary>Construct a SetCommentEvent</summary>
		/// <param name="comment">comment the comment we'll set on the {@link Criteria}
		/// when the event fires.</param>
		public SetCommentEvent(String comment)
		{
			this.comment = comment;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.SetComment(comment);
		}
	}
}
