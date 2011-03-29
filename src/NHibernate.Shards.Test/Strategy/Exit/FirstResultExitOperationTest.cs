using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Shards.Strategy.Exit;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{
    [TestFixture]
    public class FirstResultExitOperationTest
    {
        [Test]
        public void TestApply()
        {
            var input = CreateList<object>(1, 2, null, 3, 4, 5);
            VerifyFirstResultOperation(1, input, CreateList<object>(2, 3, 4, 5), "FirstResult = 1");
            VerifyFirstResultOperation(2, input, CreateList<object>(3, 4, 5), "FirstResult = 2");
        }

        [Test]
        public void TestApplyWhenFirstResultIsTooBig()
        {
            var input = CreateList<object>(1, 2, null, 3, 4, 5);
            VerifyFirstResultOperation(9, input, CreateList<object>(), "FirstResult > result set size");
            VerifyFirstResultOperation(input.Count, input, CreateList<object>(), "FirstResult = result set size");
        }

        [Test]
        public void TestApplyWhenNoResults()
        {
            VerifyFirstResultOperation(9, CreateList<object>(), CreateList<object>(), "Empty result set");
            VerifyFirstResultOperation(9, CreateList<object>(null, null, null), CreateList<object>(), "Result set with nulls only");
        }

        private static IList<T> CreateList<T>(params T[] input)
        {
            return input;
        }

        private static void VerifyFirstResultOperation<T>(int firstResult, IList<T> input, IList<T> expected, string description)
        {
            var listExitOperation = new ListExitOperation(null, firstResult, false, null, null);
            var result = listExitOperation.Execute(input).ToList();
            Assert.That(result, Is.EqualTo(expected), description);
        }
    }
}
