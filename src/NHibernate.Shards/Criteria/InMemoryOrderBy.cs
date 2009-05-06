using NHibernate.Criterion;

namespace NHibernate.Shards.Criteria
{
	public class InMemoryOrderBy
	{
		private readonly string expression;
		private readonly bool isAscending;

		///<summary>Constructs an InMemoryOrderBy instance</summary>
		/// <param name="associationPath">The association path leading to the object to which
		/// the provided {@link Order} parameter applies.  Null if the {@link Order}
		/// parameter applies to the top level object
		///</param>
		/// <param name="associationPath"></param>
		/// <param name="order">A standard Hibernate {@link Order} object.</param>
		public InMemoryOrderBy(string associationPath, Order order)
		{
			expression = GetAssociationPrefix(associationPath) + GetSortingProperty(order);
			isAscending = IsAscending(order);
		}

		private static string GetAssociationPrefix(string associationPath)
		{
			return associationPath == null ? "" : associationPath + ".";
		}

		private static bool IsAscending(Order order)
		{
			return order.ToString().ToUpper().EndsWith("ASC");
		}

		public string GetExpression()
		{
			return expression;
		}

		public bool IsAscending()
		{
			return isAscending;
		}

		private static string GetSortingProperty(Order order)
		{
			/**
             * This method relies on the format that Order is using:
             * propertyName + ' ' + (ascending?"asc":"desc")
             */
			string str = order.ToString();
			return str.Substring(0, str.IndexOf(' '));
		}
	}
}
