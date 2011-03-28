using NHibernate.Cfg;
using NHibernate.Util;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Cfg
{
    public class ShardConfiguration: IShardConfiguration
    {
        public ShardConfiguration()
        {}

        public ShardConfiguration(Configuration config)
        {
            Preconditions.CheckNotNull(config);
            this.DefaultSchema = PropertiesHelper.GetString(NHibernate.Cfg.Environment.DefaultSchema, config.Properties, null);
            this.ConnectionString = PropertiesHelper.GetString(NHibernate.Cfg.Environment.ConnectionString, config.Properties, null);
            this.ConnectionStringName = PropertiesHelper.GetString(NHibernate.Cfg.Environment.ConnectionStringName, config.Properties, null);
            this.ShardCacheRegionPrefix = PropertiesHelper.GetString(NHibernate.Cfg.Environment.CacheRegionPrefix, config.Properties, null);
            this.ShardSessionFactoryName = PropertiesHelper.GetString(NHibernate.Cfg.Environment.SessionFactoryName, config.Properties, null);
            this.ShardId = PropertiesHelper.GetInt32(ShardedEnvironment.ShardIdProperty, config.Properties, 0);
        }

        public string DefaultSchema { get; set; }
        public string ShardSessionFactoryName { get; set; }
        public int ShardId { get; set; }
        public string ShardCacheRegionPrefix { get; set; }
        public string ConnectionString { get; set; }
        public string ConnectionStringName { get; set; }
    }
}
