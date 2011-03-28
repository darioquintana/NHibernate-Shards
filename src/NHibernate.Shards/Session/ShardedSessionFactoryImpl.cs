using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Iesi.Collections.Generic;
using NHibernate.Cache;
using NHibernate.Cfg;
using NHibernate.Connection;
using NHibernate.Context;
using NHibernate.Dialect.Function;
using NHibernate.Engine;
using NHibernate.Engine.Query;
using NHibernate.Exceptions;
using NHibernate.Id;
using NHibernate.Metadata;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Id;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Util;
using NHibernate.Stat;
using NHibernate.Transaction;
using NHibernate.Type;

namespace NHibernate.Shards.Session
{
    public class ShardedSessionFactoryImpl : IShardedSessionFactoryImplementor, IControlSessionProvider
    {
        #region Static fields

        // the id of the control shard
        private const int CONTROL_SHARD_ID = 0;

        // our lovely logger
        private static readonly IInternalLogger Log = LoggerProvider.LoggerFor(typeof(ShardedSessionFactoryImpl));

        #endregion

        #region Instance fields

        // All classes that cannot be directly saved
        private readonly HashSet<System.Type> classesWithoutTopLevelSaveSupport;

        // map of SessionFactories used by this ShardedSessionFactory (might be a subset of all SessionFactories)
        private readonly Dictionary<ISessionFactoryImplementor, ICollection<ShardId>> shardIdsBySessionFactory;

        // The strategy we use for all shard-related operations
        private readonly IShardStrategy shardStrategy;

        // Reference to the SessionFactory we use for functionality that expects
        // data to live in a single, well-known location (like distributed sequences)
        private readonly ISessionFactoryImplementor controlSessionFactory;

        // flag to indicate whether we should do full cross-shard relationship
        // checking (very slow)
        private readonly bool checkAllAssociatedObjectsForDifferentShards;

        // Statistics aggregated across all contained SessionFactories
        private readonly IStatistics statistics = new StatisticsImpl();

        #endregion

        #region Ctor

        /// <summary>
        /// Constructs a ShardedSessionFactoryImpl
        /// </summary>
        /// <param name="sessionFactoryShardIdMap">Mapping of SessionFactories to shard ids. 
        ///  When using virtual shards, this map associates SessionFactories (physical
        ///  shards) with virtual shards (shard ids).  Map cannot be empty.
        ///  Map keys cannot be null.  Map values cannot be null or empty.</param>
        /// <param name="shardStrategyFactory">factory that knows how to create the <see cref="IShardStrategy"/> 
        ///  that will be used for all shard-related operations</param>
        /// <param name="classesWithoutTopLevelSaveSupport"> All classes that cannot be saved
        ///  as top-level objects</param>
        /// <param name="checkAllAssociatedObjectsForDifferentShards">Flag that controls
        ///  whether or not we do full cross-shard relationshp checking (very slow)</param>
        public ShardedSessionFactoryImpl(
            IDictionary<ISessionFactoryImplementor, ICollection<ShardId>> shardIdsBySessionFactory,
            IShardStrategyFactory shardStrategyFactory,
            IEnumerable<System.Type> classesWithoutTopLevelSaveSupport,
            bool checkAllAssociatedObjectsForDifferentShards)
        {
            Preconditions.CheckNotNull(shardIdsBySessionFactory);
            Preconditions.CheckNotNull(shardStrategyFactory);
            Preconditions.CheckNotNull(classesWithoutTopLevelSaveSupport);

            this.shardIdsBySessionFactory = new Dictionary<ISessionFactoryImplementor, ICollection<ShardId>>(shardIdsBySessionFactory);
            this.classesWithoutTopLevelSaveSupport = new HashSet<System.Type>(classesWithoutTopLevelSaveSupport);
            this.checkAllAssociatedObjectsForDifferentShards = checkAllAssociatedObjectsForDifferentShards;

            var uniqueShardIds = new HashSet<ShardId>();
            foreach (var entry in shardIdsBySessionFactory)
            {
                ISessionFactoryImplementor implementor = entry.Key;
                Preconditions.CheckNotNull(implementor);

                var shardIdSet = entry.Value;
                Preconditions.CheckNotNull(shardIdSet);
                Preconditions.CheckState(!(shardIdSet.Count == 0));

                foreach (ShardId shardId in shardIdSet)
                {
                    //TODO: we should change it so we specify control shard in configuration
                    if (shardId.Id == CONTROL_SHARD_ID)
                    {
                        this.controlSessionFactory = implementor;
                    }
                    if (!uniqueShardIds.Add(shardId))
                    {
                        string msg = string.Format("Cannot have more than one shard with shard id {0}.", shardId.Id);
                        Log.Error(msg);
                        throw new HibernateException(msg);
                    }

                    if (!this.shardIdsBySessionFactory.ContainsKey(implementor))
                    {
                        this.shardIdsBySessionFactory.Add(implementor, new HashSet<ShardId>());
                    }
                    this.shardIdsBySessionFactory[implementor].Add(shardId);
                }
            }

            if (this.controlSessionFactory == null)
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Cannot find control shard. Please ensure that one control shard exists with shard id '{0}'. " +
                    "A control shard is required for operations that cannot be distributed across shards, such as the " +
                    "generation of unique sequence numbers within the shard.",
                    CONTROL_SHARD_ID);
                Log.Error(message);
                throw new ArgumentException(message, "shardIdsBySessionFactory");
            }

            // now that we have all our shard ids, construct our shard strategy
            shardStrategy = shardStrategyFactory.NewShardStrategy(
                shardIdsBySessionFactory.SelectMany(c => c.Value));
            SetupIdGenerators();
        }

        protected ShardedSessionFactoryImpl(
            ShardedSessionFactoryImpl parent,
            IEnumerable<ShardId> shardIds,
            IShardStrategyFactory shardStrategyFactory)
        {
            this.shardIdsBySessionFactory = new Dictionary<ISessionFactoryImplementor, ICollection<ShardId>>();
            this.classesWithoutTopLevelSaveSupport = parent.classesWithoutTopLevelSaveSupport;
            this.checkAllAssociatedObjectsForDifferentShards = parent.checkAllAssociatedObjectsForDifferentShards;
            this.controlSessionFactory = parent.controlSessionFactory;

            var uniqueShardIds = new HashSet<ShardId>(shardIds);
            foreach (var pair in parent.shardIdsBySessionFactory)
            {
                var shardIdsSubset = new HashSet<ShardId>(pair.Value);
                shardIdsSubset.IntersectWith(uniqueShardIds);
                if (shardIdsSubset.Count > 0)
                {
                    this.shardIdsBySessionFactory.Add(pair.Key, shardIdsSubset);
                }
            }

            shardStrategy = shardStrategyFactory.NewShardStrategy(
                shardIdsBySessionFactory.SelectMany(c => c.Value));
        }

        /**
         * Sets the {@link ControlSessionProvider} on id generators that implement the
         * {@link GeneratorRequiringControlSessionProvider} interface
         */
        private void SetupIdGenerators()
        {
            foreach (var factory in this.shardIdsBySessionFactory.Keys)
            {
                foreach (var classMetaData in factory.GetAllClassMetadata().Values)
                {
                    var entityPersister = factory.GetEntityPersister(classMetaData.EntityName);
                    var idGenerator = entityPersister.IdentifierGenerator as IGeneratorRequiringControlSessionProvider;
                    if (idGenerator != null)
                    {
                        idGenerator.SetControlSessionProvider(this);
                    }
                }
            }
        }

        #endregion

        #region IControlSessionProvider Members

        /// <summary>
        /// Opens control session.
        /// </summary>
        /// <returns>control session</returns>
        public ISession OpenControlSession()
        {
            return this.controlSessionFactory.OpenSession();
        }

        #endregion

        #region IShardedSessionFactoryImplementor Members

        public ISessionFactoryImplementor ControlFactory
        {
            get { return this.controlSessionFactory; }
        }

        public IEnumerable<IShardMetadata> GetShardMetadata()
        {
            return this.shardIdsBySessionFactory
                .Select(p => (IShardMetadata)new ShardMetadataImpl(p.Value, p.Key));
        }

        public bool ContainsFactory(ISessionFactoryImplementor factory)
        {
            return SessionFactories.Contains(factory);
        }

        public IEnumerable<ISessionFactoryImplementor> SessionFactories
        {
            get { return this.shardIdsBySessionFactory.Keys; }
        }

        /// <summary>
        /// This method is provided to allow a client to work on a subset of
        /// shards or a specialized <see cref="IShardStrategyFactory"/>.  By providing
        /// the desired shardIds, the client can limit operations to these shards.
        /// Alternatively, this method can be used to create a ShardedSessionFactory
        /// with different strategies that might be appropriate for a specific operation.
        ///
        /// The factory returned will not be stored as one of the factories that would
        /// be returned by a call to getSessionFactories.
        /// </summary>
        /// <param name="shardIds"></param>
        /// <param name="shardStrategyFactory"></param>
        /// <returns>specially configured ShardedSessionFactory</returns>
        public IShardedSessionFactory GetSessionFactory(IEnumerable<ShardId> shardIds, IShardStrategyFactory shardStrategyFactory)
        {
            return new Subset(this, shardIds, shardStrategyFactory);
        }

        /// <summary>
        /// Create database connection(s) and open a ShardedSession on it,
        /// specifying an interceptor.
        /// </summary>
        /// <param name="interceptor">a session-scoped interceptor</param>
        /// <returns></returns>
        /// Throws <see cref="HibernateException"/>
        public IShardedSession OpenSession(IInterceptor interceptor)
        {
            return new ShardedSessionImpl(
                this,
                shardStrategy,
                classesWithoutTopLevelSaveSupport,
                interceptor,
                checkAllAssociatedObjectsForDifferentShards);
        }

        /// <summary>
        /// Create database connection and open a <c>ISession</c> on it, specifying an interceptor
        /// Warning: this interceptor will be shared across all shards, so be very
        /// careful about using a stateful implementation.
        /// </summary>
        /// <param name="interceptor">A session-scoped interceptor</param>
        /// <returns>A session</returns>
        ISession ISessionFactory.OpenSession(IInterceptor interceptor)
        {
            return OpenSession(interceptor);
        }

        /// <summary>
        /// Create database connection(s) and open a ShardedSession on it.
        /// </summary>
        /// <returns></returns>
        /// Throws <see cref="HibernateException"/>
        public IShardedSession OpenSession()
        {
            return new ShardedSessionImpl(
                this,
                shardStrategy,
                classesWithoutTopLevelSaveSupport,
                null,
                checkAllAssociatedObjectsForDifferentShards);
        }

        /// <summary>
        /// Create a database connection and open a <c>ISession</c> on it
        /// </summary>
        /// <returns></returns>
        ISession ISessionFactory.OpenSession()
        {
            return OpenSession();
        }

        /// <summary>
        /// Open a <c>ISession</c> on the given connection
        /// </summary>
        /// <param name="conn">A connection provided by the application</param>
        /// <returns>A session</returns>
        /// <remarks>
        /// Note that the second-level cache will be disabled if you
        /// supply a ADO.NET connection. NHibernate will not be able to track
        /// any statements you might have executed in the same transaction.
        /// Consider implementing your own <see cref="IConnectionProvider" />.
        /// </remarks>
        ISession ISessionFactory.OpenSession(IDbConnection conn)
        {
            throw new NotSupportedException("Cannot open a sharded session with a user provided connection.");
        }

        ISession ISessionFactory.OpenSession(IDbConnection conn, IInterceptor interceptor)
        {
            throw new NotSupportedException("Cannot open a sharded session with a user provided connection.");
        }

        /// <summary>
        /// Get the <c>ClassMetadata</c> associated with the given entity class
        /// </summary>
        /// <param name="persistentType"></param>
        /// <returns></returns>
        public IClassMetadata GetClassMetadata(System.Type persistentType)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetClassMetadata(persistentType);
        }

        public IClassMetadata GetClassMetadata(string entityName)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetClassMetadata(entityName);
        }

        /// <summary>
        /// Get the <c>CollectionMetadata</c> associated with the named collection role
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public ICollectionMetadata GetCollectionMetadata(string roleName)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetCollectionMetadata(roleName);
        }

        IDictionary<string, IClassMetadata> ISessionFactory.GetAllClassMetadata()
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetAllClassMetadata();
        }

        IDictionary<string, ICollectionMetadata> ISessionFactory.GetAllCollectionMetadata()
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetAllCollectionMetadata();
        }

        /// <summary>
        /// Destroy this <c>SessionFactory</c> and release all resources 
        /// connection pools, etc). It is the responsibility of the application
        /// to ensure that there are no open <c>Session</c>s before calling
        /// <c>close()</c>. 
        /// </summary>
        public virtual void Close()
        {
            foreach (var sessionFactory in this.shardIdsBySessionFactory.Keys)
            {
                sessionFactory.Close();
            }

            this.shardIdsBySessionFactory.Clear();
            this.classesWithoutTopLevelSaveSupport.Clear();
            this.statistics.Clear();
        }

        /// <summary>
        /// Evict all entries from the process-level cache.  This method occurs outside
        /// of any transaction; it performs an immediate "hard" remove, so does not respect
        /// any transaction isolation semantics of the usage strategy.  Use with care.
        /// </summary>
        /// <param name="persistentClass"></param>
        public void Evict(System.Type persistentClass)
        {
            foreach (var factory in SessionFactories)
            {
                factory.Evict(persistentClass);
            }
        }

        /// <summary>
        /// Evict an entry from the process-level cache.  This method occurs outside
        /// of any transaction; it performs an immediate "hard" remove, so does not respect
        /// any transaction isolation semantics of the usage strategy.  Use with care.
        /// </summary>
        /// <param name="persistentClass"></param>
        /// <param name="id"></param>
        public void Evict(System.Type persistentClass, object id)
        {
            foreach (ISessionFactoryImplementor factory in SessionFactories)
            {
                factory.Evict(persistentClass, id);
            }
        }

        /// <summary> 
        /// Evict all entries from the second-level cache. This method occurs outside
        /// of any transaction; it performs an immediate "hard" remove, so does not respect
        /// any transaction isolation semantics of the usage strategy. Use with care.
        /// </summary>
        public void EvictEntity(string entityName)
        {
            foreach (ISessionFactoryImplementor factory in SessionFactories)
            {
                factory.EvictEntity(entityName);
            }
        }

        public void EvictEntity(string entityName, object id)
        {
            foreach (ISessionFactory sf in SessionFactories)
            {
                sf.EvictEntity(entityName, id);
            }
        }

        /// <summary>
        /// Evict all entries from the process-level cache.  This method occurs outside
        /// of any transaction; it performs an immediate "hard" remove, so does not respect
        /// any transaction isolation semantics of the usage strategy.  Use with care.
        /// </summary>
        /// <param name="roleName"></param>
        public void EvictCollection(string roleName)
        {
            foreach (ISessionFactoryImplementor factory in SessionFactories)
            {
                factory.EvictCollection(roleName);
            }
        }

        /// <summary>
        /// Evict an entry from the process-level cache.  This method occurs outside
        /// of any transaction; it performs an immediate "hard" remove, so does not respect
        /// any transaction isolation semantics of the usage strategy.  Use with care.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="id"></param>
        public void EvictCollection(string roleName, object id)
        {
            foreach (ISessionFactoryImplementor factory in SessionFactories)
            {
                factory.EvictCollection(roleName, id);
            }
        }

        /// <summary>
        /// Evict any query result sets cached in the default query cache region.
        /// </summary>
        public void EvictQueries()
        {
            foreach (ISessionFactoryImplementor factory in SessionFactories)
            {
                factory.EvictQueries();
            }
        }

        /// <summary>
        /// Evict any query result sets cached in the named query cache region.
        /// </summary>
        /// <param name="cacheRegion"></param>
        public void EvictQueries(string cacheRegion)
        {
            foreach (ISessionFactoryImplementor factory in SessionFactories)
            {
                factory.EvictQueries(cacheRegion);
            }
        }

        /// <summary>
        /// Get the <see cref="IConnectionProvider" /> used.
        /// </summary>
        public IConnectionProvider ConnectionProvider
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.ConnectionProvider; }
        }

        public string TryGetGuessEntityName(System.Type implementor)
        {
            return ControlFactory.TryGetGuessEntityName(implementor);
        }

        /// <summary>
        /// Get the SQL <c>Dialect</c>
        /// </summary>
        public Dialect.Dialect Dialect
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.Dialect; }
        }

        public IInterceptor Interceptor
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.Interceptor; }
        }

        public bool IsClosed
        {
            get
            {
                // a ShardedSessionFactory is closed if any of its SessionFactories are closed
                foreach (ISessionFactory sf in SessionFactories)
                {
                    if (sf.IsClosed) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Obtain a set of the names of all filters defined on this SessionFactory.
        /// </summary>
        /// <return>The set of filter names.</return>
        public ICollection<string> DefinedFilterNames
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.DefinedFilterNames; }
        }

        /// <summary>
        /// Obtain the definition of a filter by name.
        /// </summary>
        /// <param name="filterName">The name of the filter for which to obtain the definition.</param>
        /// <return>The filter definition.</return>
        public FilterDefinition GetFilterDefinition(string filterName)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetFilterDefinition(filterName);
        }

        public Settings Settings
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.Settings; }
        }

        /// <summary>
        /// Unsupported
        /// </summary>
        public ISession GetCurrentSession()
        {
            throw new NotSupportedException();
        }

        /// <summary> Get a new stateless session.</summary>
        public IStatelessSession OpenStatelessSession()
        {
            throw new NotSupportedException();
        }

        /// <summary> Get a new stateless session for the given ADO.NET connection.</summary>
        public IStatelessSession OpenStatelessSession(IDbConnection connection)
        {
            throw new NotSupportedException("Cannot open a stateless sharded session with a user provided connection");
        }

        /// <summary> Get the statistics for this session factory</summary>
        public IStatistics Statistics
        {
            get { return statistics; }
        }

        ///<summary>
        ///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///</summary>
        ///<filterpriority>2</filterpriority>
        public void Dispose()
        {
            // try to be helpful to apps that don't clean up properly
            if (!this.IsClosed)
            {
                try
                {
                    this.Close();
                }
                catch (Exception e)
                {
                    Log.Warn("Caught exception trying to close.", e);
                }
            }
        }

        IDictionary<string, ICache> ISessionFactoryImplementor.GetAllSecondLevelCacheRegions()
        {
            return ControlFactory.GetAllSecondLevelCacheRegions();
        }

        /// <summary>
        /// Get the persister for the named class
        /// </summary>
        /// <param name="className">The name of the class that is persisted.</param>
        /// <returns>The <see cref="IEntityPersister"/> for the class.</returns>
        /// <exception cref="MappingException">If no <see cref="IEntityPersister"/> can be found.</exception>
        public IEntityPersister GetEntityPersister(string className)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetEntityPersister(className);
        }

        /// <summary>
        /// Get the persister object for a collection role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public ICollectionPersister GetCollectionPersister(string role)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetCollectionPersister(role);
        }

        /// <summary>
        /// Get the return types of a query
        /// </summary>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IType[] GetReturnTypes(string queryString)
        {
            return ControlFactory.GetReturnTypes(queryString);
        }

        /// <summary> Get the return aliases of a query</summary>
        public string[] GetReturnAliases(string queryString)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetReturnAliases(queryString);
        }

        /// <summary>
        /// Get the names of all persistent classes that implement/extend the given interface/class
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public string[] GetImplementors(string className)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetImplementors(className);
        }

        /// <summary>
        /// Get a class name, using query language imports
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetImportedClassName(string name)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetImportedClassName(name);
        }

        /// <summary>
        /// Get the default query cache
        /// </summary>
        public IQueryCache QueryCache
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.QueryCache; }
        }

        /// <summary>
        /// Get a particular named query cache, or the default cache
        /// </summary>
        /// <param name="regionName">the name of the cache region, or null for the default
        /// query cache</param>
        /// <returns>the existing cache, or a newly created cache if none by that
        /// region name</returns>
        public IQueryCache GetQueryCache(string regionName)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetQueryCache(regionName);
        }

        public ResultSetMappingDefinition GetResultSetMapping(string resultSetRef)
        {
            return ControlFactory.GetResultSetMapping(resultSetRef);
        }

        public IIdentifierGenerator GetIdentifierGenerator(string rootEntityName)
        {
            // since all configs are same, we return any
            return ControlFactory.GetIdentifierGenerator(rootEntityName);
        }

        public ITransactionFactory TransactionFactory
        {
            get { return ControlFactory.TransactionFactory; }
        }

        public ISQLExceptionConverter SQLExceptionConverter
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.SQLExceptionConverter; }
        }

        public SQLFunctionRegistry SQLFunctionRegistry
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.SQLFunctionRegistry; }
        }

        public IEntityNotFoundDelegate EntityNotFoundDelegate
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.EntityNotFoundDelegate; }
        }

        /// <summary>
        /// Gets the ICurrentSessionContext instance attached to this session factory.
        /// </summary>
        public ICurrentSessionContext CurrentSessionContext
        {
            get { return ControlFactory.CurrentSessionContext; }
        }

        /// <summary>
        ///  Unsupported.  This is a technical decision.  See <see cref="OpenSession(System.Data.IDbConnection)"/> for an explanation.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="flushBeforeCompletionEnabled"></param>
        /// <param name="autoCloseSessionEnabled"></param>
        /// <param name="connectionReleaseMode"></param>
        /// <returns></returns>
        public ISession OpenSession(IDbConnection connection, bool flushBeforeCompletionEnabled, bool autoCloseSessionEnabled,
                                    ConnectionReleaseMode connectionReleaseMode)
        {
            throw new NotSupportedException();
        }

        ISet<string> ISessionFactoryImplementor.GetCollectionRolesByEntityParticipant(string entityName)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetCollectionRolesByEntityParticipant(entityName);
        }

        public IEntityPersister TryGetEntityPersister(string entityName)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.TryGetEntityPersister(entityName);
        }

        /// <summary> The cache of table update timestamps</summary>
        public UpdateTimestampsCache UpdateTimestampsCache
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.UpdateTimestampsCache; }
        }

        /// <summary> Get a named second-level cache region</summary>
        public ICache GetSecondLevelCacheRegion(string regionName)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetSecondLevelCacheRegion(regionName);
        }

        public NamedQueryDefinition GetNamedQuery(string queryName)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetNamedQuery(queryName);
        }

        public NamedSQLQueryDefinition GetNamedSQLQuery(string queryName)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetNamedSQLQuery(queryName);
        }

        /// <summary> Statistics SPI</summary>
        public IStatisticsImplementor StatisticsImplementor
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.StatisticsImplementor; }
        }

        public QueryPlanCache QueryPlanCache
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            get { return ControlFactory.QueryPlanCache; }
        }

        public IType GetIdentifierType(string className)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetIdentifierType(className);
        }

        public string GetIdentifierPropertyName(string className)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetIdentifierPropertyName(className);
        }

        public IType GetReferencedPropertyType(string className, string propertyName)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.GetReferencedPropertyType(className, propertyName);
        }

        public bool HasNonIdentifierPropertyNamedId(string className)
        {
            // assumption is that all session factories are configured the same way,
            // so it doesn't matter which session factory answers this question
            return ControlFactory.HasNonIdentifierPropertyNamedId(className);
        }

        #endregion

        #region Inner classes

        private class Subset : ShardedSessionFactoryImpl
        {
            public Subset(
                ShardedSessionFactoryImpl parent,
                IEnumerable<ShardId> shardIds,
                IShardStrategyFactory shardStrategyFactory)
                : base(parent, shardIds, shardStrategyFactory)
            { }

            /**
             * This method is a NO-OP. As a ShardedSessionFactoryImpl that represents
             * a subset of the application's shards, it will not close any shard's
             * sessionFactory.
             *
             * @throws HibernateException
             */
            public override void Close()
            {
                // no-op: this class should never close session factories
            }
        }

        #endregion
    }
}