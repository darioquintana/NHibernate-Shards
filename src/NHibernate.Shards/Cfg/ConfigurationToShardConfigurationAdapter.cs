using NHibernate.Cfg;

namespace NHibernate.Shards.Cfg
{
	public class ConfigurationToShardConfigurationAdapter : IShardConfiguration
	{
		private readonly Configuration config;

		public ConfigurationToShardConfigurationAdapter(Configuration config)
		{
			this.config = config;
		}

		public string ShardSessionFactoryName
		{
			get { return config.GetProperty(Environment.SessionFactoryName); }
		}

		public int ShardId
		{
			get { return int.Parse(config.GetProperty(ShardedEnvironment.ShardIdProperty)); }
		}


		public string ShardCacheRegionPrefix
		{
			get { return config.GetProperty(Environment.CacheRegionPrefix); }
		}

		public string ConnectionString
		{
			get { return config.GetProperty(Environment.ConnectionString); }
		}

		public string ConnectionStringName
		{
			get { return config.GetProperty(Environment.ConnectionStringName); }
		}

		public Configuration Configuration
		{
			get { return config; }
		}
	}
}