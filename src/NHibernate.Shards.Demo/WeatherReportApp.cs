using System;
using System.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Criterion;
using NHibernate.Mapping;
using NHibernate.Shards.Cfg;
using NHibernate.Shards.Strategy;

namespace NHibernate.Shards.Test.Example
{
	internal class WeatherReportApp
	{
		private ISessionFactory sessionFactory;

		public static void Main(string[] args)
		{
			var app = new WeatherReportApp();
			app.Run();
		}

		private void Run()
		{
			CreateSchema();
			sessionFactory = CreateSessionFactory();

			AddData();

			ISession session = sessionFactory.OpenSession();
			try
			{
				ICriteria crit = session.CreateCriteria("weather", "WeatherReport");
				var count = crit.List() as List;
				if (count != null) Console.WriteLine(count.BatchSize); //.size()
				crit.Add(Restrictions.Gt("temperature", 33));
				var reports = crit.List() as List;
				if (reports != null) Console.WriteLine(reports.BatchSize);
			}
			finally
			{
				session.Close();
			}
		}

		private void CreateSchema()
		{
			for (int i = 0; i < 3; i++)
			{
				//DestroyDatabase(i, IdGenType.SIMPLE);
				//CreateDatabase(i, IdGenType.SIMPLE);
			}
			throw new NotImplementedException();
		}

		private void AddData()
		{
			ISession session = sessionFactory.OpenSession();
			try
			{
				session.BeginTransaction();
				var report = new WeatherReport
				             	{
				             		Continent = "North America",
				             		Latitude = 25,
				             		Longitude = 30,
				             		ReportTime = new DateTime(),
				             		Temperature = 44
				             	};
				session.Save(report);

				report = new WeatherReport
				         	{
				         		Continent = "Africa",
				         		Latitude = 44,
				         		Longitude = 99,
				         		ReportTime = new DateTime(),
				         		Temperature = 31
				         	};
				session.Save(report);

				report = new WeatherReport
				         	{
				         		Continent = "Asia",
				         		Latitude = 13,
				         		Longitude = 12,
				         		ReportTime = new DateTime(),
				         		Temperature = 104
				         	};
				session.Save(report);
				session.Transaction.Commit();
			}
			finally
			{
				session.Close();
			}
		}


		public ISessionFactory CreateSessionFactory()
		{
			Configuration prototypeConfig = new Configuration().Configure("hibernate0.cfg.xml");
			prototypeConfig.AddXmlFile("weather.hbm.xml");
			IList<IShardConfiguration> shardConfigs = new List<IShardConfiguration>();
			//shardConfigs.Add(new ConfigurationToShardConfigurationAdapter(new Configuration().Configure("hibernate0.cfg.xml")));
			//shardConfigs.Add(new ConfigurationToShardConfigurationAdapter(new Configuration().Configure("hibernate1.cfg.xml")));
			//shardConfigs.Add(new ConfigurationToShardConfigurationAdapter(new Configuration().Configure("hibernate2.cfg.xml")));
			IShardStrategyFactory shardStrategyFactory = BuildShardStrategyFactory();
			var shardedConfig = new ShardedConfiguration(
				prototypeConfig,
				shardConfigs,
				shardStrategyFactory);
			return shardedConfig.buildShardedSessionFactory();
		}

		private IShardStrategyFactory BuildShardStrategyFactory()
		{
			throw new NotImplementedException();
		}
	}
}