using System;
using NHibernate.Shards.Strategy.Exit;
using NSubstitute;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{

    [TestFixture]
	public class FirstNonNullResultExitStrategyTest
	{
		public void TestAddResult()
		{
			UniqueResultExitStrategy<object> fnnres = new UniqueResultExitStrategy<object>(null);
			IShard shard1 = Substitute.For<IShard>();

			fnnres.AddResult(null, shard1);
			Assert.IsNull(fnnres.CompileResults());
			//Assert.IsNull(fnnres.GetShardOfResult());

			Object result = new Object();
			IShard shard2 = Substitute.For<IShard>();
			fnnres.AddResult(result, shard2);
			Assert.AreSame(result, fnnres.CompileResults());
			//Assert.AreSame(shard2, fnnres.getShardOfResult());

			Object anotherResult = new Object();
			IShard shard3 = Substitute.For<IShard>();
			fnnres.AddResult(anotherResult, shard3);
			Assert.AreSame(result, fnnres.CompileResults());
			//Assert.AreSame(shard2, fnnres.?)getShardOfResult());
		}

		[Test]
		public void TestNullShard()
		{
		    var exitOperationFactory = Substitute.For<IExitOperationFactory>();
			UniqueResultExitStrategy<object> fnnres = new UniqueResultExitStrategy<object>(exitOperationFactory);
			try
			{
				fnnres.AddResult(null, null);
				Assert.Fail("expected npe");
			}
			catch (NullReferenceException)
			{
				// good
			}
		}
	}
}
