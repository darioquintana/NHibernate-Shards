using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NHibernate.Cfg;
using NHibernate.Shards.Cfg;
using NHibernate.Shards.LoadBalance;
using NHibernate.Shards.Session;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Strategy.Access;
using NHibernate.Shards.Strategy.Resolution;
using NHibernate.Shards.Strategy.Selection;
using NHibernate.Shards.Tool;
using NUnit.Framework;
using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Shards.Test
{
	public abstract class ShardedTestCase
	{
		#region Instance fields

		private IShardedSessionFactory _sessionFactory;

		#endregion

		#region Properties

		public IShardedSessionFactory SessionFactory
		{
			get { return _sessionFactory; }
		}

		#endregion

		#region SetUp/TearDown

		private static readonly string DataPath = Path.GetTempPath();
		private static int _prevShardId;
		private readonly List<string> _databasePaths = new List<string>();

		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			var currentAssembly = GetType().Assembly;
			var protoConfig = new Configuration()
				.SetDefaultAssembly(currentAssembly.FullName)
				.SetProperty(Environment.ConnectionDriver, typeof(MilestoneTG.NHibernate.Driver.Sqlite.Microsoft.MicrosoftSqliteDriver).AssemblyQualifiedName)
				.SetProperty(Environment.Dialect, typeof(NHibernate.Dialect.SQLiteDialect).FullName)
				.SetProperty(Environment.QuerySubstitutions, "true=1;false=0")
				.AddAssembly(currentAssembly);

			try
			{
				Configure(protoConfig);

				var shardedConfig = new ShardedConfiguration(protoConfig, CreateShardConfigurations(2), new TestStrategyFactory());
				CreateSchema(shardedConfig);
				try
				{
					_sessionFactory = shardedConfig.BuildShardedSessionFactory();
				}
				catch
				{
					DropSchema(shardedConfig);
					throw;
				}
			}
			catch (Exception)
			{
				CleanUp();
				throw;
			}
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			CleanUp();
		}

		/// <summary>
		/// Prepares this fixture for next test. Override <see cref="OnSetUp"/> to add custom setup code.
		/// </summary>
		[SetUp]
		public virtual void SetUp()
		{
			OnSetUp();
		}

		/// <summary>
		/// Extension point for test setup code.
		/// </summary>
		public virtual void OnSetUp()
		{}

		/// <summary>
		/// Cleans up this fixture after test completion. Override <see cref="OnTearDown"/> to add custom clean up code.
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			OnTearDown();

			if (!CheckDatabaseWasCleaned())
			{
				Assert.Fail("Test didn't clean up database after itself.");
			}
		}

		/// <summary>
		/// Extension point for test teardown code.
		/// </summary>
		protected virtual void OnTearDown()
		{}

		protected virtual void Configure(Configuration protoConfig)
		{ }

		protected virtual void CreateSchema(ShardedConfiguration shardedConfiguration)
		{
			new ShardedSchemaExport(shardedConfiguration).Create(false, true);
		}

		protected virtual void DropSchema(ShardedConfiguration shardedConfiguration)
		{
			new ShardedSchemaExport(shardedConfiguration).Drop(false, true);
		}

		private IEnumerable<ShardConfiguration> CreateShardConfigurations(short shardCount)
		{
			for (short shardId = 0; shardId < shardCount; shardId++)
			{
				yield return new ShardConfiguration
					{
						ShardId = shardId,
						ConnectionString = $"Data Source={CreateDatabasePath()};"
					};
			}
		}

		private string CreateDatabasePath()
		{
			var result = Path.Combine(DataPath, "shard" + Interlocked.Increment(ref _prevShardId) + ".db");
			_databasePaths.Add(result);
			return result;
		}

		private void CleanUp()
		{
			if (_sessionFactory != null)
			{
				_sessionFactory.Close();
				_sessionFactory = null;
			}

			foreach (var databasePath in _databasePaths)
			{
				if (File.Exists(databasePath)) File.Delete(databasePath);
			}
		}

		protected virtual bool CheckDatabaseWasCleaned()
		{
			if (_sessionFactory.GetAllClassMetadata().Count == 0)
			{
				// Return early in the case of no mappings, also avoiding
				// a warning when executing the HQL below.
				return true;
			}

			using (ISession s = _sessionFactory.OpenSession())
			{
				var objects = s.CreateQuery("from System.Object o").List();
				return objects.Count == 0;
			}
		}

		private class TestStrategyFactory: IShardStrategyFactory
		{
			public IShardStrategy NewShardStrategy(IEnumerable<ShardId> shardIds)
			{
				var loadBalancer = new RoundRobinShardLoadBalancer(shardIds);
				return new ShardStrategyImpl(
					new RoundRobinShardSelectionStrategy(loadBalancer),
					new AllShardsShardResolutionStrategy(shardIds), 
					new ParallelShardAccessStrategy());
			}
		}

		#endregion
	}
}
