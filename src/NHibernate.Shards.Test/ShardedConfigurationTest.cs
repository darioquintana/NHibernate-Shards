using NHibernate.Cfg;
using NHibernate.Shards.Cfg;
using NHibernate.Shards.Test.Mock;
using NUnit.Framework;

namespace NHibernate.Shards.Test
{
    [TestFixture]
    public class ShardedConfigurationTest : TestFixtureBaseWithMock
    {
        //private MyShardStrategyFactory shardStrategyFactory;
        private IShardConfiguration shardConfig;
        private ShardedConfiguration shardedConfiguration;
        protected new void SetUp() {
            //super.setUp();

            //shardStrategyFactory = new MyShardStrategyFactory();
            Configuration protoConfig = new Configuration();
            //protoConfig.SetProperty(Environment.DIALECT, HSQLDialect.class.getName());
            //shardConfig = new MyShardConfig("user", "url", "pwd", "sfname", "prefix", 33);

            //shardedConfiguration =
            //    new ShardedConfiguration(
            //        protoConfig//,
                    //Collections.singletonList(shardConfig),
                    //shardStrategyFactory);
          }
    }  
}