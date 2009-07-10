using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using log4net;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.Shards.Cfg;
using NHibernate.Shards.Session;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Util;
using NHibernate.Util;
using Environment=NHibernate.Cfg.Environment;

namespace NHibernate.Shards
{
	/// <summary>
	/// Like regular Hibernate's Configuration, this class helps construct your
	/// factories. Not extending Hibernate's Configuration because that is the one place
	/// where the notion of a single database is specified (i.e. in the
	/// hibernate.properties file). While we would like to maintain the Hibernate paradigm
	/// as much as possible, this is one place it might be different.
	/// </summary>
	public class ShardedConfiguration
	{
		private readonly ILog log = LogManager.GetLogger(typeof (ShardedConfiguration));

		// the prototype config that we'll use when constructing the shard-specific
		// configs
		private readonly Configuration prototypeConfiguration;

		// shard-specific configs
		private readonly IList<IShardConfiguration> shardConfigs;

		// user-defined sharding behavior
		private readonly IShardStrategyFactory shardStrategyFactory;

		// maps virtual shard ids to physical shard ids

		// maps physical shard ids to sets of virtual shard ids
		private readonly Dictionary<int, Set<ShardId>> shardToVirtualShardIdMap;
		private readonly Dictionary<int, int> virtualShardToShardMap;

		#region Ctors

		public ShardedConfiguration(Configuration prototypeConfiguration,
		                            IList<IShardConfiguration> shardConfigs,
		                            IShardStrategyFactory shardStrategyFactory)
			:
				this(prototypeConfiguration, shardConfigs, shardStrategyFactory, new Dictionary<int, int>())
		{
		}

		public ShardedConfiguration(Configuration prototypeConfiguration, IList<IShardConfiguration> shardConfigs,
		                            IShardStrategyFactory shardStrategyFactory, Dictionary<int, int> virtualShardToShardMap)
		{
			Preconditions.CheckNotNull(prototypeConfiguration);
			Preconditions.CheckNotNull(shardConfigs);
			Preconditions.CheckArgument(!(shardConfigs.Count == 0));
			Preconditions.CheckNotNull(shardStrategyFactory);
			Preconditions.CheckNotNull(virtualShardToShardMap);

			this.prototypeConfiguration = prototypeConfiguration;
			this.shardConfigs = shardConfigs;
			this.shardStrategyFactory = shardStrategyFactory;
			this.virtualShardToShardMap = virtualShardToShardMap;

			if (!(virtualShardToShardMap.Count == 0))
			{
				// build the map from shard to set of virtual shards
				shardToVirtualShardIdMap = new Dictionary<int, Set<ShardId>>();

				foreach (var pair in virtualShardToShardMap)
				{
					Set<ShardId> set = shardToVirtualShardIdMap[(pair.Value)];
					// see if we already have a set of virtual shards
					if (set == null)
					{
						// we don't, so create it and add it to the map
						set = new HashedSet<ShardId>();
						shardToVirtualShardIdMap.Add(pair.Value, set);
					}
					set.Add(new ShardId(pair.Key));
				}
			}
			else
			{
				shardToVirtualShardIdMap = new Dictionary<int, Set<ShardId>>();
			}
		}

		#endregion

		public IShardedSessionFactory buildShardedSessionFactory()
		{
			var sessionFactories = new Dictionary<ISessionFactoryImplementor, Set<ShardId>>();
			// since all configs get their mappings from the prototype config, and we
			// get the set of classes that don't support top-level saves from the mappings,
			// we can get the set from the prototype and then just reuse it.
			Set<System.Type> classesWithoutTopLevelSaveSupport =
				DetermineClassesWithoutTopLevelSaveSupport(prototypeConfiguration);

			foreach (IShardConfiguration config in shardConfigs)
			{
				PopulatePrototypeWithVariableProperties(config);
				// get the shardId from the shard-specific config
				int shardId = config.ShardId;

				//TODO: here HS check if shardId is not null and throw an exception

				Set<ShardId> virtualShardIds;
				if (virtualShardToShardMap.Count == 0)
				{
					// simple case, virtual and physical are the same
					virtualShardIds = new HashedSet<ShardId>(new[] {new ShardId(shardId)});
				}
				else
				{
					// get the set of shard ids that are mapped to the physical shard
					// described by this config
					virtualShardIds = shardToVirtualShardIdMap[shardId];
				}
				sessionFactories.Add(BuildSessionFactory(), virtualShardIds);
			}
			bool doFullCrossShardRelationshipChecking =
				PropertiesHelper.GetBoolean(ShardedEnvironment.CHECK_ALL_ASSOCIATED_OBJECTS_FOR_DIFFERENT_SHARDS,
				                            prototypeConfiguration.Properties, true);

			return new ShardedSessionFactoryImpl(
				sessionFactories,
				shardStrategyFactory,
				classesWithoutTopLevelSaveSupport,
				doFullCrossShardRelationshipChecking);
		}

		private ISessionFactoryImplementor BuildSessionFactory()
		{
			return (ISessionFactoryImplementor)prototypeConfiguration.BuildSessionFactory();
		}

		private void PopulatePrototypeWithVariableProperties(IShardConfiguration config)
		{
			safeSet(prototypeConfiguration, Environment.ConnectionString, config.ConnectionString);
			safeSet(prototypeConfiguration, Environment.CacheRegionPrefix, config.ShardCacheRegionPrefix);
			safeSet(prototypeConfiguration, Environment.SessionFactoryName, config.ShardSessionFactoryName);
			safeSet(prototypeConfiguration, ShardedEnvironment.ShardIdProperty, config.ShardId.ToString());
		}

		private static void safeSet(Configuration config, String key, String value)
		{
			if (value != null)
				config.SetProperty(key, value);
		}

		private Set<System.Type> DetermineClassesWithoutTopLevelSaveSupport(Configuration configuration)
		{
			Set<System.Type> classesWithoutTopLevelSaveSupport = new HashedSet<System.Type>();

			foreach (PersistentClass classMapping in configuration.ClassMappings)
			{
				foreach (Property property in classMapping.PropertyClosureIterator)
				{
					if (DoesNotSupportTopLevelSave(property))
					{
						System.Type mappedClass = classMapping.MappedClass;
						log.InfoFormat("Type {0} does not support top-level saves.", mappedClass.Name);
						classesWithoutTopLevelSaveSupport.Add(mappedClass);
						break;
					}
				}
			}

			return classesWithoutTopLevelSaveSupport;
		}


		private bool DoesNotSupportTopLevelSave(Property property)
		{
			return property.Value != null && property.Value.GetType().IsAssignableFrom(typeof (OneToOne));
		}
	}
}