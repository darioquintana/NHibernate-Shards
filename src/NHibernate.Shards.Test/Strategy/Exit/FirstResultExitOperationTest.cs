using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Shards.Strategy.Exit;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{
	[TestFixture]
	public class FirstResultExitOperationTest
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
			FirstResultExitOperation exitOp = new FirstResultExitOperation(1);

			var list = new List<object> {1, 2, null, 3, 4, 5};

			IList objects = exitOp.Apply(list);
			Assert.AreEqual(4, objects.Count);

			AssertNoNullElements(objects);
			Assert.AreEqual(new List<object> {2, 3, 4, 5}, objects);
			exitOp = new FirstResultExitOperation(2);

			list = new List<object> {1, 2, null, 3, 4, 5};

			objects = exitOp.Apply(list);
			Assert.AreEqual(3, objects.Count);
			AssertNoNullElements(objects);
			Assert.AreEqual(new List<object> {3, 4, 5}, objects);
		}

		[Test]
		public void TestApplyWhenFirstResultIsTooBig()
		{
			FirstResultExitOperation exitOp = new FirstResultExitOperation(9);

			List<Object> list = new List<object> {1, 2, null, 3, 4, 5};

			IList objects = exitOp.Apply(list);
			Assert.IsEmpty(objects);

			// edge case
			exitOp = new FirstResultExitOperation(list.Count);
			objects = exitOp.Apply(list);
			Assert.IsEmpty(objects);
		}

		[Test]
		public void TestApplyWhenNoResults()
		{
			FirstResultExitOperation exitOp = new FirstResultExitOperation(9);

			List<Object> list = new List<object>();

			IList objects = exitOp.Apply(list);
			Assert.IsEmpty(objects);

			Object nullObj = null;
			list = new List<object> {nullObj, nullObj, nullObj};

			objects = exitOp.Apply(list);
			Assert.IsEmpty(objects);
		}
	}
}
