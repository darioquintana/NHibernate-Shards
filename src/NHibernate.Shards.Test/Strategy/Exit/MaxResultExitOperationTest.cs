using System.Collections;
using System.Linq;
using NHibernate.Shards.Strategy.Exit;
using NUnit.Framework;
using System.Collections.Generic;

namespace NHibernate.Shards.Test.Strategy.Exit
{

    [TestFixture]
    public class MaxResultExitOperationTest
    {
        [Test]
        public void TestApply()
        {
            var input = CreateList<object>(1, 2, null, 3, 4, 5);
            VerifyMaxResultsOperation(3, input, CreateList<object>(1, 2, 3), "MaxResults = 3");
        }

        [Test]
        public void TestApplyWithFewerElementsThanMaxResults()
        {
            var input = CreateList<object>(1, 2, null, 3, 4, 5);
            VerifyMaxResultsOperation(8, input, CreateList<object>(1, 2, 3, 4, 5), "MaxResults > result set size");
        }

        private static IList<T> CreateList<T>(params T[] input)
        {
            return input;
        }

        private static void VerifyMaxResultsOperation<T>(int maxResults, IList<T> input, IList<T> expected, string description)
        {
            var listExitOperation = new ListExitOperation(maxResults, 0, false, null, null);
            var result = listExitOperation.Execute(input).ToList();
            Assert.That(result, Is.EqualTo(expected), description);
        }
    }
}
