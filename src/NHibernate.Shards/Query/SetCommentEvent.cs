using System;

namespace NHibernate.Shards.Query
{
	public class SetCommentEvent : IQueryEvent
	{
		private String comment;

		public SetCommentEvent(String comment)
		{
			this.comment = comment;
		}

		public void OnEvent(IQuery query)
		{
			query.SetComment(comment);
		}

	}
}
