namespace NHibernate.Shards.Criteria
{
    public class SetFlushModeEvent : ICriteriaEvent
    {
          private readonly FlushMode flushMode;

          /**
           * Construct a SetFlushModeEvent
           *
           * @param flushMode the flushMode that
           * we'll set on the {@link Criteria} when the event fires.
           */
          public SetFlushModeEvent(FlushMode flushMode) {
            this.flushMode = flushMode;
          }

          public void OnEvent(ICriteria crit) {
            crit.SetFlushMode(flushMode);
          }

    }
}
