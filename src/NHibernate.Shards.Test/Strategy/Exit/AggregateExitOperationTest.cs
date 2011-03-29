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
            VerifyAggregateListExitOperation(AggregationUtil.Max, data, new object[] { 5 }, "Max");
        }

        [Test]
        public void TestMin()
        {
            VerifyAggregateListExitOperation(AggregationUtil.Min, data, new object[] { 0 }, "Max");
        }

        [Test]
        public void TestSum()
        {
            VerifyAggregateListExitOperation(AggregationUtil.GetSumFunc(typeof(int)), data, new object[] { 11 }, "Sum");
        }

        private static void VerifyAggregateListExitOperation<T>(AggregationFunc aggregation, IList<T> input, IList<T> expected, string description)
        {
            var listExitOperation = new ListExitOperation(null, 0, false, aggregation, null);
            var result = listExitOperation.Execute(input);
            Assert.That(result, Is.EqualTo(expected), description);
        }
    }
}
