namespace NHibernate.Shards.Criteria
{
    public class SetMaxResultsEvent : ICriteriaEvent
    {
          // the maxResults we'll set when the event fires
          private readonly int maxResults;

          /**
           * Constructs a SetMaxResultsEvent
           *
           * @param maxResults the maxResults we'll set on the {@link Criteria} when
           * the event fires.
           */
          public SetMaxResultsEvent(int maxResults) {
            this.maxResults = maxResults;
          }

          public void OnEvent(ICriteria crit) {
            crit.SetMaxResults(maxResults);
          }

          public int GetMaxResults() {
            return maxResults;
          }

            }
}
