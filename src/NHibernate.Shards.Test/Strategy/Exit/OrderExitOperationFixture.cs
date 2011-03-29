using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Criterion;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{
    using System.Linq;

    [TestFixture]
    public class OrderExitOperationFixture : TestFixtureBaseWithMock
    {
        private IList<object> data;
        private IList<object> shuffledList;
        private IList<object> nonNullData;

        private class MyInt
        {
            private readonly int i;

            private readonly String name;

            private MyInt innerMyInt;

            public MyInt(int i, String name)
            {
                this.i = i;
                this.name = name;
            }

            public MyInt InnerMyInt
            {
                get { return innerMyInt; }
                set { innerMyInt = value; }
            }

            public long Value
            {
                get { return i; }
            }

            public String Name
            {
                get { return name; }
            }

            public override bool Equals(Object obj)
            {
                MyInt myInt = (MyInt)obj;
                return this.Name.Equals(myInt.Name) && this.Value.Equals(myInt.Value);
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
        }


        protected override void OnSetUp()
        {
            var names = new[] { "tomislav", "max", "maulik", "gut", "null", "bomb" };
            data = Enumerable
                .Range(0, 6)
                .Select(i => i != 4 ? (object)new MyInt(i, names[i]) : null)
                .ToList();

            nonNullData = data.OfType<object>().Where(o => o != null).ToList();
            shuffledList = Collections.RandomList(nonNullData);
        }

        [Test]
        public void Apply()
        {
            var orders = new[] { SortOrder.Ascending("Value") };
            VerifyOrderedListExitOperation(orders, shuffledList, nonNullData, "Sort ascending on one property");
        }

        [Test]
        public void MultipleOrderings()
        {
            var orders = new[]
                {
                    SortOrder.Ascending("Value"),
                    SortOrder.Ascending("Name")
                };
            var expected = new object[]
             	{
             		new MyInt(0, "tomislav"),
             		new MyInt(1, "max"),
             		new MyInt(2, "maulik"),
             		new MyInt(3, "gut"),
             		new MyInt(5, "bomb")
             	};
            VerifyOrderedListExitOperation(orders, shuffledList, expected, "Sort on two properties");
        }

        private static void VerifyOrderedListExitOperation<T>(IEnumerable<SortOrder> orders, IList<T> input, IList<T> expected, string description)
        {
            var comparer = new SortOrderComparer(orders);
            var listExitOperation = new ListExitOperation(null, 0, false, null, comparer);
            var result = listExitOperation.Execute(input);
            Assert.That(result, Is.EqualTo(expected) & Is.Ordered.Using((IComparer)comparer), description);
        }
    }
}