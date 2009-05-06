using NHibernate.Criterion;

namespace NHibernate.Shards.Criteria
{
	public class AddOrderEvent : ICriteriaEvent
	{
		private readonly Order order;

		///<summary>Construct an AddOrderEvent</summary>
		///<param name="order">the Order we'll add when the event fires</param>
		public AddOrderEvent(Order order)
		{
			this.order = order;
		}

		public void OnEvent(ICriteria crit)
		{
			crit.AddOrder(order);
		}
	}
}
