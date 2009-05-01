
namespace NHibernate.Shards.Criteria
{
    public class SetFetchSizeEvent : ICriteriaEvent
    {
          // the fetchSize we'll set on the Criteria when the event fires.
          private readonly int fetchSize;

          /**
           * Construct a SetFetchSizeEvent
           *
           * @param fetchSize the fetchSize that
           * we'll set on the {@link Criteria} when the event fires.
           */
          public SetFetchSizeEvent(int fetchSize) {
            this.fetchSize = fetchSize;
          }

          public void OnEvent(ICriteria crit) {
            crit.SetFetchSize(fetchSize);
          }

    }
}
