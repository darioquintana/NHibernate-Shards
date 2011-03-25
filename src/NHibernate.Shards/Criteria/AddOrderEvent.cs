using NHibernate.Criterion;

namespace NHibernate.Shards.Criteria
{
	/// <summary>
	/// Event that allows an Order to be lazily added to a Criteria.
	/// </summary>
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
