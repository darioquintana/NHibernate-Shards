
namespace NHibernate.Shards.Criteria
{
    public class SetTimeoutEvent : ICriteriaEvent
    {
        private readonly int timeout;

        /**
         * Constructs a SetTimeoutEvent
         *
         * @param timeout the timeout we'll set on the {@link Criteria} when the
         * event fires.
         */
        public SetTimeoutEvent(int timeout) 
        {
          this.timeout = timeout;
        }

        public void OnEvent(ICriteria crit) 
        {
          crit.SetTimeout(timeout);
        }
    }
}
