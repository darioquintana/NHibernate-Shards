using System;
using System.Collections;
using NHibernate.Criterion;

namespace NHibernate.Shards.Strategy.Exit
{
	public class AggregateExitOperation : IProjectionExitOperation
	{
		public enum SupportedAggregations
		{
			SUM,
			MIN,
			MAX
		}

		private readonly SupportedAggregations _aggregate;
		private readonly string _fieldName;

		public IList Apply(IList results)
		{
			IList nonNullResults = ExitOperationUtils.GetNonNullList(results);
			switch (_aggregate.ToString().ToLower())
			{
				case "max":
					return ExitOperationUtils.GetMaxList(nonNullResults);
				case "min":
					return ExitOperationUtils.GetMinList(nonNullResults);
				case "sum":
					IList sumList = new ArrayList();
					sumList.Add(GetSum(results, _fieldName));
					return sumList;
				default:
					throw new NotSupportedException("Aggregation Projected is unsupported: " + _aggregate);
			}
		}

		private Decimal GetSum(IList results, string fieldName)
		{
			Decimal sum = new Decimal();
			foreach (object obj in results)
			{
				double num = GetNumber(obj, fieldName);
				sum += new decimal(num);
			}
			return sum;
		}

		private static double GetNumber(object obj, string fieldName)
		{
			return Double.Parse(ExitOperationUtils.GetPropertyValue(obj, fieldName).ToString());
		}

		public string Aggregate
		{
			get { return _aggregate.ToString(); }
		}

		public AggregateExitOperation(IProjection projection)
		{
			string projectionAsString = projection.ToString();
			int start = projectionAsString.IndexOf("(");
			string aggregateName = projectionAsString.Substring(0, start);
			start++;
			int stop = projectionAsString.IndexOf(")");
			_fieldName = projectionAsString.Substring(start, stop - start);
			_aggregate = (SupportedAggregations) Enum.Parse(_aggregate.GetType(), aggregateName.ToUpper());
		}
	}
}