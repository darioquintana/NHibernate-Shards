using System;

namespace NHibernate.Shards.Criteria
{
    public class SetCommentEvent : ICriteriaEvent
    {
          private readonly String comment;

          /**
           * Construct a SetCommentEvent
           *
           * @param comment the comment we'll set on the {@link Criteria}
           * when the event fires.
           */
          public SetCommentEvent(String comment) {
            this.comment = comment;
          }

          public void OnEvent(ICriteria crit) {
            crit.SetComment(comment);
          }

    }
}
