using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Criterion;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Threading.Exception;
using NHibernate.Type;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{
	[TestFixture]
	public class AvgResultsExitOperationTest
	{
		[Test,Ignore]
		public void TestAvgProjectionComesBackAsDouble()
		{
			// sharded avg calculation assumes that the avg projection implementation
			// returns a Double, so let's make sure that assumption is valid
			AvgProjection ap = new AvgProjection("yam");
			IType[] types = ap.GetTypes(null, null);
			Assert.NotNull(types);
			Assert.AreEqual(1, types.Length);
			Assert.AreEqual(typeof (DoubleType), types[0]);
		}

		[Test]
		public void TestBadInput()
		{
			AvgResultsExitOperation op = new AvgResultsExitOperation();
			Object[] objArr = { null };
			
			try
			{
				op.Apply(new List<object>{objArr});
				Assert.Fail("expected rte");
			}
			catch (IllegalStateException rte)
			{
				// good
			}

			try
			{
				op.Apply(new List<object> {new object()});
				Assert.Fail("expected rte");
			}
			catch (IllegalStateException rte)
			{
				// good
			}
		}

		[Test]
		public void TestEmptyList()
		{
			AvgResultsExitOperation op = new AvgResultsExitOperation();

			IList result = op.Apply(new List<object>());
			Assert.AreEqual(1, result.Count);
			Assert.IsNull(result[0]);
		}

		[Test]
		public void TestMultipleResults()
		{
			AvgResultsExitOperation op = new AvgResultsExitOperation();

			Object[] objArr1 = { null, 3 };
			Object[] objArr2 = { 2.5, 2 };

			IList result = op.Apply(new List<object> { objArr1, objArr2});
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(2.5, result[0]);

			objArr1[0] = 2.0;

			result = op.Apply(new List<object> {objArr1, objArr2});
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(2.2, result[0]);
		}

		[Test]
		public void TestSingleResult()
		{
			AvgResultsExitOperation op = new AvgResultsExitOperation();

			Object[] objArr = { null, 3 };

			IList result = op.Apply(new List<object> {objArr});
			Assert.AreEqual(1, result.Count);
			Assert.IsNull(result[0]);

			objArr[0] = 9.0;

			result = op.Apply(new List<object>{objArr});
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(9.0, result[0]);
		}
	}
}
