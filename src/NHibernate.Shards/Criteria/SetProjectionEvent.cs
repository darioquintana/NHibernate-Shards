using NHibernate.Criterion;

namespace NHibernate.Shards.Criteria
{
    public class SetProjectionEvent : ICriteriaEvent
    {
          private readonly IProjection projection;

          /**
           * Constructs a SetProjectionEvent
           *
           * @param projection the projection we'll set on the {@link Criteria} when the
           * event fires.
           */
          public SetProjectionEvent(IProjection projection) {
            this.projection = projection;
          }


          public void OnEvent(ICriteria crit) {
            crit.SetProjection(projection);
          }

    }
}
