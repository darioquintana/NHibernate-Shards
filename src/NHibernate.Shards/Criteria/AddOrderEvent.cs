using NHibernate.Criterion;

namespace NHibernate.Shards.Criteria
{
    public class AddOrderEvent : ICriteriaEvent
    {
        private readonly Order order;

        /**
         * Construct an AddOrderEvent
         *
         * @param order the Order we'll add when the event fires
         */
        public AddOrderEvent(Order order) {
          this.order = order;
        }
        
        public void OnEvent(ICriteria crit) {
          crit.AddOrder(order);
        }
    }
}
