using System;
using System.Collections.Generic;
using System.Reflection;
using NHibernate.ByteCode.LinFu;
using NHibernate.Cfg;
using NHibernate.Cfg.Loquacious;
using NHibernate.Criterion;
using NHibernate.Dialect;
using NHibernate.Shards.Cfg;
using NHibernate.Shards.LoadBalance;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Strategy.Access;
using NHibernate.Shards.Strategy.Resolution;
using NHibernate.Shards.Strategy.Selection;
using NHibernate.Shards.Tool;

namespace NHibernate.Shards.Demo
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
            var shardedConfiguration = BuildShardedConfiguration();
            CreateSchema(shardedConfiguration);
            sessionFactory = shardedConfiguration.BuildShardedSessionFactory();

            AddData();

            ISession session = sessionFactory.OpenSession();
            try
            {
                ICriteria crit = session.CreateCriteria(typeof(WeatherReport), "weather");
                var count = crit.List();
                if (count != null) Console.WriteLine(count.Count);
                crit.Add(Restrictions.Gt("Temperature", 33));
                var reports = crit.List();
                if (reports != null) Console.WriteLine(reports.Count);
            }
            finally
            {
                session.Close();
            }

            sessionFactory.Dispose();
            Console.WriteLine("Done.");
            Console.ReadKey(true);
        }

        private static void CreateSchema(ShardedConfiguration shardedConfiguration)
        {
            var shardedSchemaExport = new ShardedSchemaExport(shardedConfiguration);
            shardedSchemaExport.Drop(false, true);
            shardedSchemaExport.Create(false, true);
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
                    ReportTime = DateTime.Now,
                    Temperature = 44
                };
                session.Save(report);

                report = new WeatherReport
                {
                    Continent = "Africa",
                    Latitude = 44,
                    Longitude = 99,
                    ReportTime = DateTime.Now,
                    Temperature = 31
                };
                session.Save(report);

                report = new WeatherReport
                {
                    Continent = "Asia",
                    Latitude = 13,
                    Longitude = 12,
                    ReportTime = DateTime.Now,
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

        public ShardedConfiguration BuildShardedConfiguration()
        {
            var prototypeConfig = new Configuration()
                .Proxy(p =>
                {
                    p.Validation = false;
                    p.ProxyFactoryFactory<ProxyFactoryFactory>();
                })
                .DataBaseIntegration(db =>
                {
                    db.Dialect<MsSql2008Dialect>();
                })
                .AddResource("NHibernate.Shards.Demo.Mappings.hbm.xml", Assembly.GetExecutingAssembly());

            var shardConfigs = BuildShardConfigurations();
            var shardStrategyFactory = BuildShardStrategyFactory();
            return new ShardedConfiguration(prototypeConfig, shardConfigs, shardStrategyFactory);
        }

        private IEnumerable<IShardConfiguration> BuildShardConfigurations()
        {
            for (short i = 0; i < 3; i++)
            {
                yield return new ShardConfiguration
                {
                    ShardSessionFactoryName = "Shard" + i,
                    ShardId = i,
                    ConnectionStringName = "shard" + i
                };
            }
        }

        private static IShardStrategyFactory BuildShardStrategyFactory()
        {
            return new MyStrategy();
        }
    }

    public class MyStrategy : IShardStrategyFactory
    {
        #region IShardStrategyFactory Members

        public IShardStrategy NewShardStrategy(IEnumerable<ShardId> shardIds)
        {
            var loadBalancer = new RoundRobinShardLoadBalancer(shardIds);
            var pss = new RoundRobinShardSelectionStrategy(loadBalancer);
            IShardResolutionStrategy prs = new AllShardsShardResolutionStrategy(shardIds);
            IShardAccessStrategy pas = new SequentialShardAccessStrategy();
            return new ShardStrategyImpl(pss, prs, pas);
        }

        #endregion
    }
}