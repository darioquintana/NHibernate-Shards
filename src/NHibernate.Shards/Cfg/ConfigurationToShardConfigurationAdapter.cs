using NHibernate.Cfg;

namespace NHibernate.Shards.Cfg
{
	public class ConfigurationToShardConfigurationAdapter
	{
		private readonly Configuration config;

		public ConfigurationToShardConfigurationAdapter(Configuration config)
		{
			this.config = config;
		}

		//public string getShardUrl()
		//{
		//    return config.GetProperty(Environment.URL);
		//}

		//public string getShardUser()
		//{
		//    return config.GetProperty(Environment.USER);
		//}

		//public string getShardPassword()
		//{
		//    return config.GetProperty(Environment.PASS);
		//}

		public string ShardSessionFactoryName
		{
			get {return config.GetProperty(Environment.SessionFactoryName);}
		}

		public int ShardId
		{
			get { return int.Parse(config.GetProperty(ShardedEnvironment.ShardIdProperty)); }
		}

		//public string ShardDatasource
		//{
		//    get {return config.GetProperty(Environment.DATASOURCE);}
		//}

		public string ShardCacheRegionPrefix
		{
			get { return config.GetProperty(Environment.CacheRegionPrefix); }
		}
	}
}