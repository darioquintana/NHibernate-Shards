using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.Shards.Cfg;
using NHibernate.Shards.Session;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Util;
using NHibernate.Util;
using Environment = NHibernate.Cfg.Environment;

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
        private readonly static IInternalLogger Log = LoggerProvider.LoggerFor(typeof(ShardedConfiguration));

        // the prototype config that we'll use when constructing the shard-specific
        // configs
        private readonly Configuration prototypeConfiguration;

        // shard-specific configs
        private readonly IList<IShardConfiguration> shardConfigs;

        // user-defined sharding behavior
        private readonly IShardStrategyFactory shardStrategyFactory;

        // maps virtual shard ids to physical shard ids
        private readonly Dictionary<short, short> virtualShardToShardMap;

        // maps physical shard ids to sets of virtual shard ids
        private readonly Dictionary<short, ICollection<ShardId>> shardToVirtualShardIdMap;

        #region Ctors

        public ShardedConfiguration(Configuration prototypeConfiguration, IEnumerable<IShardConfiguration> shardConfigs, IShardStrategyFactory shardStrategyFactory)
            : this(prototypeConfiguration, shardConfigs, shardStrategyFactory, new Dictionary<short, short>())
        { }

        public ShardedConfiguration(
            Configuration prototypeConfiguration,
            IEnumerable<IShardConfiguration> shardConfigs,
            IShardStrategyFactory shardStrategyFactory,
            Dictionary<short, short> virtualShardToShardMap)
        {
            Preconditions.CheckNotNull(prototypeConfiguration);
            Preconditions.CheckNotNull(shardConfigs);
            Preconditions.CheckNotNull(shardStrategyFactory);
            Preconditions.CheckNotNull(virtualShardToShardMap);

            this.prototypeConfiguration = prototypeConfiguration;
            this.shardConfigs = shardConfigs.ToList();
            Preconditions.CheckArgument(this.shardConfigs.Count > 0);

            this.shardStrategyFactory = shardStrategyFactory;
            this.virtualShardToShardMap = virtualShardToShardMap;

            if (!(virtualShardToShardMap.Count == 0))
            {
                // build the map from shard to set of virtual shards
                shardToVirtualShardIdMap = new Dictionary<short, ICollection<ShardId>>();

                foreach (var pair in virtualShardToShardMap)
                {
                    var set = shardToVirtualShardIdMap[(pair.Value)];
                    // see if we already have a set of virtual shards
                    if (set == null)
                    {
                        // we don't, so create it and add it to the map
                        set = new HashSet<ShardId>();
                        shardToVirtualShardIdMap.Add(pair.Value, set);
                    }
                    set.Add(new ShardId(pair.Key));
                }
            }
            else
            {
                shardToVirtualShardIdMap = new Dictionary<short, ICollection<ShardId>>();
            }
        }

        #endregion

        #region Public methods

        public IShardedSessionFactory BuildShardedSessionFactory()
        {
            var sessionFactories = new Dictionary<ISessionFactoryImplementor, ICollection<ShardId>>();
            // since all configs get their mappings from the prototype config, and we
            // get the set of classes that don't support top-level saves from the mappings,
            // we can get the set from the prototype and then just reuse it.
            var classesWithoutTopLevelSaveSupport = ClassesWithoutTopLevelSaveSupport(prototypeConfiguration);

            foreach (IShardConfiguration config in shardConfigs)
            {
                PopulatePrototypeWithVariableProperties(config);
                // get the shardId from the shard-specific config
                short shardId = config.ShardId;

                //TODO: here HS check if shardId is not null and throw an exception

                ICollection<ShardId> virtualShardIds;
                if (virtualShardToShardMap.Count == 0)
                {
                    // simple case, virtual and physical are the same
                    virtualShardIds = new HashSet<ShardId> { new ShardId(shardId) };
                }
                else
                {
                    // get the set of shard ids that are mapped to the physical shard
                    // described by this config
                    virtualShardIds = shardToVirtualShardIdMap[shardId];
                }
                sessionFactories.Add(BuildSessionFactory(), virtualShardIds);
            }

            bool doFullCrossShardRelationshipChecking = PropertiesHelper.GetBoolean(
                ShardedEnvironment.CheckAllAssociatedObjectsForDifferentShards,
                prototypeConfiguration.Properties, true);

            return new ShardedSessionFactoryImpl(
                sessionFactories,
                shardStrategyFactory,
                classesWithoutTopLevelSaveSupport,
                doFullCrossShardRelationshipChecking);
        }

        internal void ForEachShard(Action<Configuration> shardConfigAction)
        {
            foreach (IShardConfiguration config in this.shardConfigs)
            {
                PopulatePrototypeWithVariableProperties(config);
                shardConfigAction(this.prototypeConfiguration);
            }
        }

        #endregion

        #region Private methods

        private ISessionFactoryImplementor BuildSessionFactory()
        {
            return (ISessionFactoryImplementor)prototypeConfiguration.BuildSessionFactory();
        }

        private void PopulatePrototypeWithVariableProperties(IShardConfiguration config)
        {
            var oldDefaultSchema = PropertiesHelper.GetString(Environment.DefaultSchema, prototypeConfiguration.Properties, null);

            SafeSet(prototypeConfiguration, Environment.ConnectionString, config.ConnectionString);
            SafeSet(prototypeConfiguration, Environment.ConnectionStringName, config.ConnectionStringName);
            SafeSet(prototypeConfiguration, Environment.CacheRegionPrefix, config.ShardCacheRegionPrefix);
            SafeSet(prototypeConfiguration, Environment.DefaultSchema, config.DefaultSchema);
            SafeSet(prototypeConfiguration, Environment.SessionFactoryName, config.ShardSessionFactoryName);
            SafeSet(prototypeConfiguration, ShardedEnvironment.ShardIdProperty, config.ShardId.ToString());

            if (config.DefaultSchema != null && config.DefaultSchema != oldDefaultSchema)
            {
                ReplaceSchema(prototypeConfiguration, oldDefaultSchema, config.DefaultSchema);
            }
        }

        private static void SafeSet(Configuration config, String key, String value)
        {
            if (value != null) config.SetProperty(key, value);
        }

        private static void ReplaceSchema(Configuration config, string fromSchema, string toSchema)
        {
            foreach (var table in AllTables(config))
            {
                if (table.Schema == fromSchema)
                {
                    table.Schema = toSchema;
                }
            }
        }

        private static IEnumerable<Table> AllTables(Configuration config)
        {
            foreach (var classMapping in config.ClassMappings)
            {
                foreach (var table in classMapping.TableClosureIterator) yield return table;
                foreach (var table in classMapping.SubclassTableClosureIterator) yield return table;
                foreach (var join in classMapping.SubclassJoinClosureIterator) yield return join.Table;
            }

            foreach (var collectionMapping in config.CollectionMappings)
            {
                if (collectionMapping.CollectionTable != null)
                {
                    yield return collectionMapping.CollectionTable;
                }
            }
        }

        private static IEnumerable<System.Type> ClassesWithoutTopLevelSaveSupport(Configuration config)
        {
            foreach (PersistentClass classMapping in config.ClassMappings)
            {
                foreach (Property property in classMapping.PropertyClosureIterator)
                {
                    if (property.Value is OneToOne)
                    {
                        System.Type mappedClass = classMapping.MappedClass;
                        Log.InfoFormat("Type {0} does not support top-level saves.", mappedClass.Name);
                        yield return mappedClass;
                        break;
                    }
                }
            }
        }

        #endregion
    }
}