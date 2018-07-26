using System.Collections.Generic;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Exit
{
	/// <summary>
	/// A builder of <see cref="ExitOperation"/> instances.
	/// </summary>
	public class ExitOperationBuilder
	{
		private readonly List<SortOrder> orders = new List<SortOrder>();

		/// <summary>
		/// Maximum number of results requested by the client.
		/// Defaults to <c>null</c>.
		/// </summary>
		public int? MaxResults { get; set; }

		/// <summary>
		/// Index of the first result requested by the client.
		/// Defaults to <c>0</c>.
		/// </summary>
		public int FirstResult { get; set; }

		/// <summary>
		/// Indication whether client requests removal of duplicate results.
		/// Defaults to <c>false</c>.
		/// </summary>
		public bool Distinct { get; set; }

		/// <summary>
		/// Optional aggregation function to be applied to the results.
		/// </summary>
		public AggregationFunc Aggregation { get; set; }

		/// <summary>
		/// Creates empty <see cref="ExitOperationBuilder"/> instance.
		/// </summary>
		public ExitOperationBuilder()
		{}

		/// <summary>
		/// Creates clone of another <see cref="ExitOperationBuilder"/> instance.
		/// </summary>
		/// <param name="other">The builder to clone.</param>
		public ExitOperationBuilder(ExitOperationBuilder other)
		{
			Preconditions.CheckNotNull(other);

			this.orders.AddRange(other.orders);
			this.MaxResults = other.MaxResults;
			this.FirstResult = other.FirstResult;
			this.Distinct = other.Distinct;
			this.Aggregation = other.Aggregation;
		}

		/// <summary>
		/// Sort order to be applied to the results.
		/// </summary>
		public IList<SortOrder> Orders
		{
			get { return this.orders; }
		}

		/// <summary>
		/// Creates new <see cref="ExitOperation"/> with settings matching those 
		/// that are currently defined on this builder instance.
		/// </summary>
		/// <returns>A new <see cref="ExitOperation"/> with settings matching those 
		/// that are currently defined on this builder instance.</returns>
		public ExitOperation BuildListOperation()
		{
			return new ExitOperation(this.MaxResults, this.FirstResult, this.Distinct, this.Aggregation, BuildComparer());
		}

		public ExitOperationBuilder Clone()
		{
			return new ExitOperationBuilder(this);
		}

		private IComparer<object> BuildComparer()
		{
			return this.orders.Count > 0
				? new SortOrderComparer(this.orders)
				: null;
		}
	}
}
