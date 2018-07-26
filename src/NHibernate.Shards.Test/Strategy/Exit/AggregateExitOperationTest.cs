using System.Collections.Generic;
using System.Linq;
using NHibernate.Shards.Strategy.Exit;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{
	[TestFixture]
	public class AggregateExitOperationTest
	{
		private IList<object> data;

		[SetUp]
		public void SetUp()
		{
			data = Enumerable
				.Range(0, 6)
				.Select(i => i != 4 ? (object)i : null)
				.ToList();
		}

		[Test]
		public void TestMax()
		{
			VerifyAggregateListExitOperation(c => c.Max(o => o), data, new object[] { 5 }, "Max");
		}

		[Test]
		public void TestMin()
		{
			VerifyAggregateListExitOperation(c => c.Min(o => o), data, new object[] { 0 }, "Max");
		}

		[Test]
		public void TestSum()
		{
			VerifyAggregateListExitOperation(c => c.SumInt64(o => o), data, new object[] { 11 }, "Sum");
		}

		private static void VerifyAggregateListExitOperation<T>(AggregationFunc aggregation, IList<T> input, IList<T> expected, string description)
		{
			var listExitOperation = new ExitOperation(null, 0, false, aggregation, null);
			var result = listExitOperation.Execute(input).ToArray();
			Assert.That(result, Is.EqualTo(expected), description);
		}
	}
}
