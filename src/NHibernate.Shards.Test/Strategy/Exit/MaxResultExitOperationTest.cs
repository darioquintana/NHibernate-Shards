using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Shards.Strategy.Exit;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{
	[TestFixture]
	public class MaxResultExitOperationTest
	{
		private static void AssertNoNullElements(IList objects)
		{
			foreach (Object obj in objects)
			{
				Assert.IsNotNull(obj);
			}
		}

		[Test]
		public void TestApply()
		{
			MaxResultsExitOperation exitOp = new MaxResultsExitOperation(3);

			List<object> list = new List<object> {1, 2, null, 3, 4, 5};

			IList objects = exitOp.Apply(list);
			Assert.AreEqual(3, objects.Count);
			AssertNoNullElements(objects);
			Assert.AreEqual(new List<object> {1, 2, 3}, objects);
		}

		[Test]
		public void TestApplyWithFewerElementsThanMaxResults()
		{
			MaxResultsExitOperation exitOp = new MaxResultsExitOperation(8);
			List<Object> list = new List<object> {1, 2, null, 3, 4, 5};
			IList objects = exitOp.Apply(list);
			Assert.AreEqual(5, objects.Count);
			AssertNoNullElements(objects);
			Assert.AreEqual(new List<object> {1, 2, 3, 4, 5}, objects);
		}
	}
}
