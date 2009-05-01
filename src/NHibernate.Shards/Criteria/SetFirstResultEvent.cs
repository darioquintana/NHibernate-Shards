
namespace NHibernate.Shards.Criteria
{
    public class SetFirstResultEvent : ICriteriaEvent
    {
          private readonly int firstResult;

          /**
           * Construct a SetFirstResultEvent
           *
           * @param firstResult the firstResult that
           * we'll set on the {@link Criteria} when the event fires.
           */
          public SetFirstResultEvent(int firstResult)
          {
            this.firstResult = firstResult;
          }

          public void OnEvent(ICriteria crit)
          {
            crit.SetFirstResult(firstResult);
          }

    }
}
