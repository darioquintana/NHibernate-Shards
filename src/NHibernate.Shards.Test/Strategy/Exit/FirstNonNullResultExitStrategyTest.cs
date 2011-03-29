using System;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Strategy.Exit
{
    [TestFixture]
    public class FirstNonNullResultExitStrategyTest : TestFixtureBaseWithMock
    {
        public void TestAddResult()
        {
            UniqueResultExitStrategy<object> fnnres = new UniqueResultExitStrategy<object>();
            IShard shard1 = Mock<IShard>();

            fnnres.AddResult(null, shard1);
            Assert.IsNull(fnnres.CompileResults());
            //Assert.IsNull(fnnres.GetShardOfResult());

            Object result = new Object();
            IShard shard2 = Mock<IShard>();
            fnnres.AddResult(result, shard2);
            Assert.AreSame(result, fnnres.CompileResults());
            //Assert.AreSame(shard2, fnnres.getShardOfResult());

            Object anotherResult = new Object();
            IShard shard3 = Mock<IShard>();
            fnnres.AddResult(anotherResult, shard3);
            Assert.AreSame(result, fnnres.CompileResults());
            //Assert.AreSame(shard2, fnnres.?)getShardOfResult());
        }

        [Test]
        public void TestNullShard()
        {
            UniqueResultExitStrategy<object> fnnres = new UniqueResultExitStrategy<object>();
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
