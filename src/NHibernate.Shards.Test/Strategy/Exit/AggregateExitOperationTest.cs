using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Criterion;
using NHibernate.Shards.Strategy.Exit;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{
	[TestFixture]
	public class AggregateExitOperationTest
	{
		private List<Object> data;

		private class MyInt : IComparable
		{
			private readonly int i;

			public MyInt(int i)
			{
				this.i = i;
			}

			public int Value
			{
				get { return i; }
			}

			#region IComparable Members

			public int CompareTo(object obj)
			{
				MyInt i = (MyInt) obj;
				return Value - i.Value;
			}

			#endregion
		}

		[SetUp]
		public void SetUp()
		{
			data = new List<object>();
			for (int i = 0; i < 6; i++)
			{
				if (i == 4)
				{
					data.Add(null);
				}
				else
				{
					data.Add(new MyInt(i));
				}
			}
		}

		[Test]
		public void TestCtor()
		{
			try
			{
				new AggregateExitOperation(new AvgProjection("foo"));
				Assert.Fail();
			}
			catch (ArgumentException e)
			{
				// good
			}
			try
			{
				new AggregateExitOperation(new AvgProjection("foo"));
				Assert.Fail();
			}
			catch (ArgumentException e)
			{
				// good
			}
			new AggregateExitOperation(Projections.Max("foo"));
			new AggregateExitOperation(Projections.Min("foo"));
			new AggregateExitOperation(Projections.Sum("foo"));
		}

		[Test]
		public void TestMax()
		{
			AggregateExitOperation exitOp = new AggregateExitOperation(Projections.Max("value"));

			IList result = exitOp.Apply(data);
			Assert.AreEqual(5, ((MyInt) result[0]).Value);
		}

		[Test]
		public void TestMin()
		{
			AggregateExitOperation exitOp = new AggregateExitOperation(Projections.Min("value"));

			IList result = exitOp.Apply(data);
			Assert.AreEqual(0, ((MyInt) result[0]).Value);
		}

		[Test,Ignore]
		public void TestSum()
		{
			AggregateExitOperation exitOp = new AggregateExitOperation(Projections.Sum("value"));

			IList result = exitOp.Apply(data);
			Assert.AreEqual(11.0m, (decimal) result[0]);
		}
	}
}
