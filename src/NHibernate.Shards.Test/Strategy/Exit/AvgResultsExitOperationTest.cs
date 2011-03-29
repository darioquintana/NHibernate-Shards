using System.Collections.Generic;
using NHibernate.Criterion;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Type;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{
    [TestFixture]
    public class AvgResultsExitOperationTest
    {
        [Test, Ignore]
        public void TestAvgProjectionComesBackAsDouble()
        {
            // sharded avg calculation assumes that the avg projection implementation
            // returns a Double, so let's make sure that assumption is valid
            AvgProjection ap = new AvgProjection("yam");
            IType[] types = ap.GetTypes(null, null);
            Assert.NotNull(types);
            Assert.AreEqual(1, types.Length);
            Assert.AreEqual(typeof(DoubleType), types[0]);
        }

        [Test]
        public void TestBadInput()
        {
            VerifyAverageListExitOperation(new object[] { null }, new object[] { null }, "Input contains null");
        }

        [Test]
        public void TestEmptyList()
        {
            VerifyAverageListExitOperation(new object[0], new object[] { null }, "Input is empty");
        }

        [Test]
        public void TestMultipleResults()
        {
            var input = new[]
		        {
                    new object[] { null, 3 },
                    new object[] { 2.5, 2 }
		        };
            VerifyAverageListExitOperation(input, new object[] { 2.5 }, "Input contains null sum");

            var input2 = new[]
		        {
                    new object[] { 2.0, 3 },
                    new object[] { 2.5, 2 }
		        };
            VerifyAverageListExitOperation(input2, new object[] { 2.2 }, "Input is normal");
        }

        [Test]
        public void TestSingleResult()
        {
            var input = new[]
		        {
                    new object[] { null, 3 },
		        };
            VerifyAverageListExitOperation(input, new object[] { null }, "Input contains null sum");

            var input2 = new[]
		        {
                    new object[] { 9.0, 3 },
		        };
            VerifyAverageListExitOperation(input2, new object[] { 9.0 }, "Input is normal");
        }

        private static void VerifyAverageListExitOperation<T>(IList<T> input, IList<T> expected, string description)
        {
            AggregationFunc averageFunc = c => c.Average(
                arr => (double?)((object[])arr)[0],
                arr => (int?)((object[])arr)[1]);
            var listExitOperation = new ListExitOperation(null, 0, false, averageFunc, null);
            var result = listExitOperation.Execute(input);
            Assert.That(result, Is.EqualTo(expected), description);
        }
    }
}
