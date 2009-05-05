﻿//using System;
//using System.Collections.Generic;
//using Iesi.Collections.Generic;
//using LinFu.DynamicProxy;
//using NHibernate.Mapping;
//using NHibernate.Persister.Entity;
//using NHibernate.Shards.Session;
//using NHibernate.Shards.Strategy;
//using NHibernate.Engine;
//using NHibernate.Shards.Util;
//using NHibernate.Stat;
//using log4net;

//namespace NHibernate.Shards.Criteria
//{
//    public class SubsetShardedSessionFactoryImpl : ShardedSessionFactoryImpl
//    {
//          // the id of the control shard
//          private static readonly int CONTROL_SHARD_ID = 0;

//          // the SessionFactoryImplementor objects to which we delegate
//          private readonly IList<ISessionFactoryImplementor> sessionFactories;

//          // All classes that cannot be directly saved
//          private readonly Set<Class<?>> classesWithoutTopLevelSaveSupport;

//          // map of SessionFactories used by this ShardedSessionFactory (might be a subset of all SessionFactories)
//          private readonly Dictionary<ISessionFactoryImplementor, Set<ShardId>> sessionFactoryShardIdMap;

//          // map of all existing SessionFactories, used when creating a new ShardedSessionFactory for some subset of shards
//          private readonly Dictionary<ISessionFactoryImplementor, Set<ShardId>> fullSessionFactoryShardIdMap;

//          // The strategy we use for all shard-related operations
//          private readonly IShardStrategy shardStrategy;

//          // Reference to the SessionFactory we use for functionality that expects
//          // data to live in a single, well-known location (like distributed sequences)
//          private readonly ISessionFactoryImplementor controlSessionFactory;

//          // flag to indicate whether we should do full cross-shard relationship
//          // checking (very slow)
//          private readonly bool checkAllAssociatedObjectsForDifferentShards;

//          // Statistics aggregated across all contained SessionFactories
//          private readonly IStatistics statistics = new StatisticsImpl(this);

//          // our lovely logger
//          private readonly ILog log = LogManager.GetLogger(getClass());

//          /**
//           * Constructs a ShardedSessionFactoryImpl
//           * @param shardIds The ids of the shards with which this SessionFactory
//           * should be associated.
//           * @param sessionFactoryShardIdMap Mapping of SessionFactories to shard ids.
//           * When using virtual shards, this map associates SessionFactories (physical
//           * shards) with virtual shards (shard ids).  Map cannot be empty.
//           * Map keys cannot be null.  Map values cannot be null or empty.
//           * @param shardStrategyFactory  factory that knows how to create the {@link ShardStrategy}
//           * that will be used for all shard-related operations
//           * @param classesWithoutTopLevelSaveSupport  All classes that cannot be saved
//           * as top-level objects
//           * @param checkAllAssociatedObjectsForDifferentShards  Flag that controls
//           * whether or not we do full cross-shard relationshp checking (very slow)
//           */
//          public ShardedSessionFactoryImpl(
//              List<ShardId> shardIds,
//              Map<SessionFactoryImplementor, Set<ShardId>> sessionFactoryShardIdMap,
//              IShardStrategyFactory shardStrategyFactory,
//              Set<Class<?>> classesWithoutTopLevelSaveSupport,
//              bool checkAllAssociatedObjectsForDifferentShards) {
//            Preconditions.CheckNotNull(sessionFactoryShardIdMap);
//            Preconditions.CheckArgument(!sessionFactoryShardIdMap.isEmpty());
//            Preconditions.CheckNotNull(shardStrategyFactory);
//            Preconditions.CheckNotNull(classesWithoutTopLevelSaveSupport);

//            sessionFactories = Lists.newArrayList(sessionFactoryShardIdMap.keySet());
//            sessionFactoryShardIdMap = Maps.newHashMap();
//            fullSessionFactoryShardIdMap = sessionFactoryShardIdMap;
//            this.classesWithoutTopLevelSaveSupport = Sets.newHashSet(classesWithoutTopLevelSaveSupport);
//            this.checkAllAssociatedObjectsForDifferentShards = checkAllAssociatedObjectsForDifferentShards;
//            Set<ShardId> uniqueShardIds = Sets.newHashSet();
//            SessionFactoryImplementor controlSessionFactoryToSet = null;
//            foreach (Map.Entry<SessionFactoryImplementor, ISet<ShardId>> entry in sessionFactoryShardIdMap.entrySet()) {
//              SessionFactoryImplementor implementor = entry.getKey();
//              Preconditions.CheckNotNull(implementor);
//              Set<ShardId> shardIdSet = entry.getValue();
//              Preconditions.CheckNotNull(shardIdSet);
//              Preconditions.CheckArgument(!shardIdSet.IsEmpty());
//              foreach (ShardId shardId in shardIdSet) {
//                // TODO(tomislav): we should change it so we specify control shard in configuration
//                if (shardId.GetId() == CONTROL_SHARD_ID) {
//                  controlSessionFactoryToSet = implementor;
//                }
//                if(!uniqueShardIds.Add(shardId)) {
//                  readonly string msg = string.Format("Cannot have more than one shard with shard id %d.", shardId.getId());
//                  log.Error(msg);
//                  throw new HibernateException(msg);
//                }
//                if (shardIds.contains(shardId)) {
//                  if (!sessionFactoryShardIdMap.containsKey(implementor)) {
//                    sessionFactoryShardIdMap.put(implementor, Sets.<ShardId>newHashSet());
//                  }
//                  sessionFactoryShardIdMap.get(implementor).add(shardId);
//                }
//              }
//            }
//            // make sure someone didn't associate a session factory with a shard id
//            // that isn't in the full list of shards
//            foreach (ShardId shardId in shardIds) {
//              Preconditions.CheckState(uniqueShardIds.Contains(shardId));
//            }
//            controlSessionFactory = controlSessionFactoryToSet;
//            // now that we have all our shard ids, construct our shard strategy
//            shardStrategy = shardStrategyFactory.newShardStrategy(shardIds);
//            setupIdGenerators();
//          }

//          /**
//           * Constructs a ShardedSessionFactoryImpl
//           * @param sessionFactoryShardIdMap Mapping of SessionFactories to shard ids.
//           * When using virtual shards, this map associates SessionFactories (physical
//           * shards) with virtual shards (shard ids).  Map cannot be empty.
//           * Map keys cannot be null.  Map values cannot be null or empty.
//           * @param shardStrategyFactory  factory that knows how to create the {@link ShardStrategy}
//           * that will be used for all shard-related operations
//           * @param classesWithoutTopLevelSaveSupport  All classes that cannot be saved
//           * as top-level objects
//           * @param checkAllAssociatedObjectsForDifferentShards  Flag that controls
//           * whether or not we do full cross-shard relationshp checking (very slow)
//           */
//          public ShardedSessionFactoryImpl(
//              Map<SessionFactoryImplementor, Set<ShardId>> sessionFactoryShardIdMap,
//              ShardStrategyFactory shardStrategyFactory,
//              Set<Class<?>> classesWithoutTopLevelSaveSupport,
//              bool checkAllAssociatedObjectsForDifferentShards) {
//            this(Lists.newArrayList(Iterables.concat(sessionFactoryShardIdMap.values())),
//                sessionFactoryShardIdMap,
//                shardStrategyFactory,
//                classesWithoutTopLevelSaveSupport,
//                checkAllAssociatedObjectsForDifferentShards);
//          }

//          /**
//           * Sets the {@link ControlSessionProvider} on id generators that implement the
//           * {@link GeneratorRequiringControlSessionProvider} interface
//           */
//          private void SetupIdGenerators() {
//            foreach(SessionFactoryImplementor sfi in sessionFactories) {
//              foreach(Object obj in sfi.getAllClassMetadata().values()) {
//                ClassMetadata cmd = (ClassMetadata) obj;
//                EntityPersister ep = sfi.getEntityPersister(cmd.getEntityName());
//                if(ep.getIdentifierGenerator() is GeneratorRequiringControlSessionProvider) {
//                  ((GeneratorRequiringControlSessionProvider)ep.getIdentifierGenerator()).setControlSessionProvider(this);
//                }
//              }
//            }
//          }
          
//          public new Dictionary<SessionFactoryImplementor, Set<ShardId>> GetSessionFactoryShardIdMap() {
//            return sessionFactoryShardIdMap;
//          }

//          /**
//           * Unsupported.  This is a technical decision.  We would need a
//           * ShardedConnection in order to make this work, but since this method is
//           * exposed on the public api we can't force clients to provide it.  And
//           * at any rate, exposing a ShardedConnection somewhat defeats the purpose
//           * of tucking away all the sharding intelligence.
//           */
//          public ISession OpenSession(Connection connection) {
//            throw new UnsupportedOperationException(
//                "Cannot open a sharded session with a user provided connection.");
//          }

//          /**
//           * Warning: this interceptor will be shared across all shards, so be very
//           * careful about using a stateful implementation.
//           */
//          public IShardedSession OpenSession(Interceptor interceptor)
//              /*throws HibernateException*/ {
//            return new ShardedSessionImpl(
//                interceptor,
//                this,
//                shardStrategy,
//                classesWithoutTopLevelSaveSupport,
//                checkAllAssociatedObjectsForDifferentShards);
//          }

//          /**
//           * Unsupported.  This is a technical decision.  See {@link ShardedSessionFactoryImpl#OpenSession(Connection)}
//           * for an explanation.
//           */
//          public ISession OpenSession(Connection connection, Interceptor interceptor) {
//            throw new UnsupportedOperationException(
//                "Cannot open a sharded session with a user provided connection.");
//          }

//          public  IShardedSession OpenSession() throws HibernateException {
//            return new ShardedSessionImpl(
//                this,
//                shardStrategy,
//                classesWithoutTopLevelSaveSupport,
//                checkAllAssociatedObjectsForDifferentShards);
//          }

//          /**
//           * Unsupported.  This is a project decision.  We'll get to it later.
//           */
//          public IStatelessSession OpenStatelessSession() {
//            throw new UnsupportedOperationException("Not yet implemented");
//          }

//          /**
//           * Unsupported.  This is a technical decision.  See {@link ShardedSessionFactoryImpl#OpenSession(Connection)}
//           * for an explanation.
//           */
//          public IStatelessSession OpenStatelessSession(Connection connection) {
//            throw new UnsupportedOperationException(
//                "Cannot open a stateless sharded session with a user provided connection");
//          }

//          public Session getCurrentSession() throws HibernateException {
//            throw new UnsupportedOperationException();
//          }

//          public ClassMetadata getClassMetadata(Class persistentClass)
//              throws HibernateException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getClassMetadata(persistentClass);
//          }

//          public ClassMetadata getClassMetadata(String entityName)
//              throws HibernateException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getClassMetadata(entityName);
//          }

//          public CollectionMetadata getCollectionMetadata(String roleName)
//              throws HibernateException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getCollectionMetadata(roleName);
//          }

//          public Map getAllClassMetadata() throws HibernateException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getAllClassMetadata();
//          }

//          public Map getAllCollectionMetadata() throws HibernateException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getAllCollectionMetadata();
//          }

//          /**
//           * Unsupported.  This is a scope decision, not a technical decision.
//           */
//          public Statistics getStatistics() {
//            return statistics;
//          }

//          public void close() throws HibernateException {
//            for(SessionFactory sf : sessionFactories) {
//              sf.close();
//            }
//            sessionFactories.clear();
//            if(classesWithoutTopLevelSaveSupport != null) {
//              classesWithoutTopLevelSaveSupport.clear();
//            }
//            if(sessionFactoryShardIdMap != null) {
//              sessionFactoryShardIdMap.clear();
//            }
//            if(fullSessionFactoryShardIdMap != null) {
//              fullSessionFactoryShardIdMap.clear();
//            }
//            statistics.clear();
//          }

//          public boolean isClosed() {
//            // a ShardedSessionFactory is closed if any of its SessionFactories are closed
//            for(SessionFactory sf : sessionFactories) {
//              if(sf.isClosed()) {
//                return true;
//              }
//            }
//            return false;
//          }

//          public void evict(Class persistentClass) throws HibernateException {
//            for(SessionFactory sf : sessionFactories) {
//              sf.evict(persistentClass);
//            }
//          }

//          public void evict(Class persistentClass, Serializable id)
//              throws HibernateException {
//            for(SessionFactory sf : sessionFactories) {
//              sf.evict(persistentClass, id);
//            }
//          }

//          public void evictEntity(String entityName) throws HibernateException {
//            for(SessionFactory sf : sessionFactories) {
//              sf.evictEntity(entityName);
//            }
//          }

//          public void evictEntity(String entityName, Serializable id)
//              throws HibernateException {
//            for(SessionFactory sf : sessionFactories) {
//              sf.evictEntity(entityName, id);
//            }
//          }

//          public void evictCollection(String roleName) throws HibernateException {
//            for(SessionFactory sf : sessionFactories) {
//              sf.evictCollection(roleName);
//            }
//          }

//          public void evictCollection(String roleName, Serializable id)
//              throws HibernateException {
//            for(SessionFactory sf : sessionFactories) {
//              sf.evictCollection(roleName, id);
//            }
//          }

//          public void evictQueries() throws HibernateException {
//            for(SessionFactory sf : sessionFactories) {
//              sf.evictQueries();
//            }
//          }

//          public void evictQueries(String cacheRegion) throws HibernateException {
//            for(SessionFactory sf : sessionFactories) {
//              sf.evictQueries(cacheRegion);
//            }
//          }

//          public Set getDefinedFilterNames() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getDefinedFilterNames();
//          }

//          public FilterDefinition getFilterDefinition(String filterName)
//              throws HibernateException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getFilterDefinition(filterName);
//          }

//          /**
//           * Unsupported.  This is a scope decision, not a technical one.
//           */
//          public Reference getReference() throws NamingException {
//            throw new UnsupportedOperationException(
//                "Sharded session factories do not support References (sorry).");
//          }

//          public IdentifierGenerator getIdentifierGenerator(String rootEntityName) {
//            // since all configs are same, we return any
//            return getAnyFactory().getIdentifierGenerator(rootEntityName);
//          }

//          public SessionImplementor openControlSession() {
//            Preconditions.checkState(controlSessionFactory != null);
//            Session session = controlSessionFactory.openSession();
//            return  (SessionImplementor)session;
//          }

//          public boolean containsFactory(SessionFactoryImplementor factory) {
//            return sessionFactories.contains(factory);
//          }

//          private SessionFactoryImplementor getAnyFactory() {
//            return sessionFactories.get(0);
//          }

//          public List<SessionFactory> getSessionFactories() {
//            return Collections.<SessionFactory>unmodifiableList(sessionFactories);
//          }

//          /**
//           * Constructs a ShardedSessionFactory that operates on the given list of
//           * shardIds. This operation is relatively lightweight as the returned
//           * ShardedSessionFactory reuses existing shards. Most common use will be to
//           * provide a ShardedSessionFactory that manages a subset of the application's
//           * shards.
//           *
//           * @param shardIds
//           * @param shardStrategyFactory
//           * @return ShardedSessionFactory
//           */
//          public ShardedSessionFactory getSessionFactory(List<ShardId> shardIds,
//              ShardStrategyFactory shardStrategyFactory) {
//            return new SubsetShardedSessionFactoryImpl(
//                shardIds,
//                fullSessionFactoryShardIdMap,
//                shardStrategyFactory,
//                classesWithoutTopLevelSaveSupport,
//                checkAllAssociatedObjectsForDifferentShards);
//          }

//          @Override
//          protected void readonlyize() throws Throwable {
//            try {
//              // try to be helpful to apps that don't clean up properly
//              if(!isClosed()) {
//                log.warn("ShardedSessionFactoryImpl is being garbage collected but it was never properly closed.");
//                try {
//                  close();
//                } catch (Exception e) {
//                  log.warn("Caught exception trying to close.", e);
//                }
//              }
//            } readonlyly {
//              super.readonlyize();
//            }
//          }

//          public EntityPersister getEntityPersister(String entityName)
//              throws MappingException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getEntityPersister(entityName);
//          }

//          public CollectionPersister getCollectionPersister(String role)
//              throws MappingException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getCollectionPersister(role);
//          }

//          public Dialect getDialect() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getDialect();
//          }

//          public Interceptor getInterceptor() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getInterceptor();
//          }

//          public QueryPlanCache getQueryPlanCache() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getQueryPlanCache();
//          }

//          public Type[] getReturnTypes(String queryString) throws HibernateException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getReturnTypes(queryString);
//          }

//          public String[] getReturnAliases(String queryString)
//              throws HibernateException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getReturnAliases(queryString);
//          }

//          public ConnectionProvider getConnectionProvider() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getConnectionProvider();
//          }

//          public String[] getImplementors(String className) throws MappingException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getImplementors(className);
//          }

//          public String getImportedClassName(String name) {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getImportedClassName(name);
//          }

//          public TransactionManager getTransactionManager() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getTransactionManager();
//          }

//          public QueryCache getQueryCache() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getQueryCache();
//          }

//          public QueryCache getQueryCache(String regionName) throws HibernateException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getQueryCache(regionName);
//          }

//          public UpdateTimestampsCache getUpdateTimestampsCache() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getUpdateTimestampsCache();
//          }

//          public StatisticsImplementor getStatisticsImplementor() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getStatisticsImplementor();
//          }

//          public NamedQueryDefinition getNamedQuery(String queryName) {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getNamedQuery(queryName);
//          }

//          public NamedSQLQueryDefinition getNamedSQLQuery(String queryName) {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getNamedSQLQuery(queryName);
//          }

//          public ResultSetMappingDefinition getResultSetMapping(String name) {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getResultSetMapping(name);
//          }

//          public Cache getSecondLevelCacheRegion(String regionName) {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getSecondLevelCacheRegion(regionName);
//          }

//          public Map getAllSecondLevelCacheRegions() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getAllSecondLevelCacheRegions();
//          }

//          public SQLExceptionConverter getSQLExceptionConverter() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getSQLExceptionConverter();
//          }

//          public Settings getSettings() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getSettings();
//          }

//          public Session openTemporarySession() throws HibernateException {
//            throw new UnsupportedOperationException();
//          }

//          /**
//           * Unsupported.  This is a technical decision.  See {@link ShardedSessionFactoryImpl#OpenSession(Connection)}
//           * for an explanation.
//           */
//          public Session openSession(readonly Connection connection,
//              readonly boolean flushBeforeCompletionEnabled,
//              readonly boolean autoCloseSessionEnabled,
//              readonly ConnectionReleaseMode connectionReleaseMode)
//              throws HibernateException {
//            throw new UnsupportedOperationException();
//          }

//          public Set getCollectionRolesByEntityParticipant(String entityName) {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getCollectionRolesByEntityParticipant(entityName);
//          }

//          public Type getIdentifierType(String className) throws MappingException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getIdentifierType(className);
//          }

//          public String getIdentifierPropertyName(String className)
//              throws MappingException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getIdentifierPropertyName(className);
//          }

//          public Type getReferencedPropertyType(String className, String propertyName)
//              throws MappingException {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getReferencedPropertyType(className, propertyName);
//          }

//          public EntityNotFoundDelegate getEntityNotFoundDelegate() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getEntityNotFoundDelegate();
//          }

//          public SQLFunctionRegistry getSqlFunctionRegistry() {
//            // assumption is that all session factories are configured the same way,
//            // so it doesn't matter which session factory answers this question
//            return getAnyFactory().getSqlFunctionRegistry();
//          }

//    }
//}
