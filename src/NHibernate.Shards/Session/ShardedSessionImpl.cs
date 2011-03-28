using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Engine;
using NHibernate.Metadata;
using NHibernate.Proxy;
using NHibernate.Shards.Criteria;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Query;
using NHibernate.Shards.Stat;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Transaction;
using NHibernate.Shards.Util;
using NHibernate.Stat;
using NHibernate.Type;
using System.Globalization;

namespace NHibernate.Shards.Session
{
    /// <summary>
    /// Concrete implementation of a ShardedSession, and also the central component of
    /// Hibernate Shards' internal implementation. This class exposes two interfaces;
    /// ShardedSession itself, to the application, and ShardedSessionImplementor, to
    /// other components of Hibernate Shards. This class is not threadsafe.
    /// </summary>
    public class ShardedSessionImpl : IShardedSession, IShardedSessionImplementor, IShardIdResolver
    {
        #region Static fields

        private static readonly IInternalLogger Log = LoggerProvider.LoggerFor(typeof(ShardedSessionImpl));

        [ThreadStatic]
        private static ShardId currentSubgraphShardId;

        #endregion

        #region Instance fields

        private readonly bool checkAllAssociatedObjectsForDifferentShards;
        private readonly HashSet<System.Type> classesWithoutTopLevelSaveSupport;

        private readonly IShardedSessionFactoryImplementor shardedSessionFactory;
        private readonly IShardStrategy shardStrategy;
        private readonly IInterceptor interceptor;

        private readonly EntityMode entityMode;
        private readonly ShardedSessionImpl rootSession;
        private Dictionary<EntityMode, IShardedSession> childSessionsByEntityMode;

        private readonly IDictionary<ShardId, IShard> shardsById = new Dictionary<ShardId, IShard>();
        private readonly IList<IShard> shards = new List<IShard>();

        // All sessions that have been opened  within the scope of this sharded session.
        private readonly IDictionary<IShard, ISession> establishedSessionsByShard = new Dictionary<IShard, ISession>();
        // Actions that are to be applied to newly opened sessions.
        private readonly IList<Action<ISession>> establishActions = new List<Action<ISession>>();

        private bool closed;
        private bool lockedShard;
        private ShardId lockedShardId;

        // Current transaction, lazily initialized.
        private IShardedTransaction transaction;
        // Synchronization lock for current transaction, primary required because
        // we cannot be sure from which thread transaction completion callback calls 
        // will arrive.
        private readonly object transactionLock = new object();

        // Aggregated statistics, lazily initialized.
        private ShardedSessionStatistics statistics;

        // Actions that are to be applied to newly opened sessions.
        private IDictionary<string, IShardedFilter> enabledFilters;

        #endregion

        #region Constructor(s)

        /**
		 * Constructor used for openSession(...) processing.
		 *
		 * @param interceptor The interceptor to be applied to this session
		 * @param shardedSessionFactory The factory from which this session was obtained
		 * @param shardStrategy The shard strategy for this session
		 * @param classesWithoutTopLevelSaveSupport The set of classes on which top-level save can not be performed
		 * @param checkAllAssociatedObjectsForDifferentShards Should we check for cross-shard relationships
		 */
        public ShardedSessionImpl(
            IShardedSessionFactoryImplementor shardedSessionFactory,
            IShardStrategy shardStrategy,
            IEnumerable<System.Type> classesWithoutTopLevelSaveSupport,
            IInterceptor interceptor,
            bool checkAllAssociatedObjectsForDifferentShards)
        {
            this.shardedSessionFactory = shardedSessionFactory;
            this.shardStrategy = shardStrategy;
            this.classesWithoutTopLevelSaveSupport = new HashSet<System.Type>(classesWithoutTopLevelSaveSupport);
            this.interceptor = interceptor;
            this.entityMode = EntityMode.Poco;
            this.checkAllAssociatedObjectsForDifferentShards = checkAllAssociatedObjectsForDifferentShards;

            foreach (var shardMetadata in shardedSessionFactory.GetShardMetadata())
            {
                var shard = new ShardImpl(this, shardMetadata);
                this.shards.Add(shard);
                foreach (var shardId in shard.ShardIds)
                {
                    this.shardsById.Add(shardId, shard);
                }
            }
        }

        private ShardedSessionImpl(ShardedSessionImpl parent, EntityMode entityMode)
        {
            this.rootSession = parent;
            this.shardedSessionFactory = parent.shardedSessionFactory;
            this.shardStrategy = parent.shardStrategy;
            this.classesWithoutTopLevelSaveSupport = parent.classesWithoutTopLevelSaveSupport;
            this.interceptor = parent.interceptor;
            this.entityMode = entityMode;
            this.checkAllAssociatedObjectsForDifferentShards = parent.checkAllAssociatedObjectsForDifferentShards;
            this.shardsById = parent.shardsById;
        }

        #endregion

        public static ShardId CurrentSubgraphShardId
        {
            get { return currentSubgraphShardId; }
            private set { currentSubgraphShardId = value; }
        }

        public IShard AnyShard
        {
            get
            {
                return this.establishedSessionsByShard.Keys.FirstOrDefault()
                    ?? this.shards[0];
            }
        }

        private ISession AnySession
        {
            get
            {
                return this.establishedSessionsByShard.Values.FirstOrDefault()
                    ?? this.shards[0].EstablishSession();
            }
        }

        #region IShardedSession Members

        /// <summary>
        /// Read-only collection of shards that are accessible to this session.
        /// </summary>
        /// <value></value>
        public IEnumerable<IShard> Shards
        {
            get { return this.shards; }
        }

        /// <summary>
        /// Registers an action to be performed once on each shard-local session
        /// that has been or will be opened within the scope of this sharded
        /// session.
        /// </summary>
        /// <param name="action">The action to be performed once on an opened
        /// shard-local session.</param>
        /// <remarks>
        /// The <see cref="action"/> is performed immediately on all shard-local
        /// sessions that have already been established. It is also scheduled for
        /// execution when any new shard-local sessions are established within the
        /// scope of this sharded session.
        /// </remarks>
        public void AddEstablishAction(Action<ISession> action)
        {
            this.establishActions.Add(action);
            foreach (var session in this.establishedSessionsByShard.Values)
            {
                action(session);
            }
        }

        /// <summary>
        /// Establishes a shard-local session for a given shard.
        /// </summary>
        /// <param name="shard">The shard for which a session is to be established.</param>
        /// <returns>
        /// An open session for the <paramref name="shard"/>.
        /// </returns>
        public ISession EstablishFor(IShard shard)
        {
            ISession result;
            if (!this.establishedSessionsByShard.TryGetValue(shard, out result))
            {
                if (this.rootSession == null)
                {
                    var sessionInterceptor = BuildSessionInterceptor();
                    result = sessionInterceptor != null
                        ? shard.SessionFactory.OpenSession(sessionInterceptor)
                        : shard.SessionFactory.OpenSession();
                }
                else
                {
                    result = this.rootSession.EstablishFor(shard).GetSession(this.entityMode);
                }

                foreach (var action in establishActions)
                {
                    action(result);
                }

                lock (this.transactionLock)
                {
                    if (this.transaction != null)
                    {
                        this.transaction.Enlist(result);
                    }
                }

                establishedSessionsByShard.Add(shard, result);
            }
            return result;
        }

        private IInterceptor BuildSessionInterceptor()
        {
            var interceptorFactory = this.interceptor as IStatefulInterceptorFactory;
            var defaultInterceptor = interceptorFactory != null
                ? interceptorFactory.NewInstance()
                : this.interceptor;

            // cross shard association checks for updates are handled using interceptors
            if (!this.checkAllAssociatedObjectsForDifferentShards) return defaultInterceptor;

            var crossShardRelationshipDetector = new CrossShardRelationshipDetector(this);
            if (defaultInterceptor == null) return crossShardRelationshipDetector;

            return new CrossShardRelationshipDetectorDecorator(
                crossShardRelationshipDetector, defaultInterceptor);
        }

        /// <summary>
        /// Gets the session for the shard with which a given object is associated.
        /// </summary>
        /// <param name="obj">the object for which we want the session.</param>
        /// <returns>
        /// The session for the shard with which this object is associated, or
        /// <c>null</c> if the object is not associated with a session belonging
        /// to this <see cref="IShardedSession"/>.
        /// </returns>
        public ISession GetSessionForObject(object obj)
        {
            foreach (var session in establishedSessionsByShard.Values)
            {
                if (session.Contains(obj)) return session;
            }
            return null;
        }

        /// <summary>
        /// Gets the ShardId of the shard with which a given object is associated.
        /// </summary>
        /// <param name="obj">A persistent object.</param>
        /// <returns>
        /// The <see cref="ShardId"/> of the shard with which this object is associated, or
        /// <c>null</c> if the object is not associated with a shard belonging
        /// to this <see cref="IShardedSession"/>.
        /// </returns>
        public ShardId GetShardIdForObject(object obj)
        {
            return obj != null
                ? GetShardIdForObject(null, obj)
                : null;
        }

        /// <summary>
        ///  Gets the ShardId of the shard with which the objects is associated.
        /// </summary>
        /// <param name="entityName">Entity name of <paramref name="entity"/>.</param>
        /// <param name="obj">the object for which we want the Session</param>
        /// <returns>
        /// the ShardId of the Shard with which this object is associated, or
        /// null if the object is not associated with a shard belonging to this
        /// ShardedSession
        /// </returns>
        public ShardId GetShardIdForObject(string entityName, object obj)
        {
            ShardId shardId;
            return TryGetShardIdForAttachedObject(entityName, obj, out shardId)
                ? shardId
                : null;
        }

        /// <summary>
        ///  Gets the ShardId of the shard with which the object is associated.
        /// </summary>
        /// <param name="entityName">Entity name of <paramref name="entity"/>.</param>
        /// <param name="obj">the object for which we want the Session</param>
        /// <returns>
        /// the ShardId of the Shard with which this object is associated, or
        /// null if the object is not associated with a shard belonging to this
        /// ShardedSession
        /// </returns>
        public bool TryGetShardIdForAttachedObject(string entityName, object obj, out ShardId result)
        {
            //TODO: optimize this by keeping an identity map of objects to shardId
            IShard shard;
            if (!TryGetShardForAttachedEntity(obj, out shard))
            {
                result = null;
                return false;
            }

            if (TryResolveToSingleShardId(shard.ShardIds, out result)) return true;

            var key = CreateKey(entityName, obj);
            if (this.shardedSessionFactory.TryExtractShardIdFromKey(key, out result)) return true;

            throw new HibernateException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Cannot resolve shard id for entity name '{0}'. Please ensure that the identifier for this " +
                    "entity type is generated by an IShardEncodingIdentifierGenerator implementation or do not " +
                    "use virtual shards.",
                    key.EntityName));
        }

        /// <summary>
        /// Place the session into a state where every create operation takes place
        /// on the same shard.  Once the shard is locked on a session it cannot
        /// be unlocked.
        /// </summary>
        public void LockShard()
        {
            lockedShard = true;
        }

        /// <summary>
        /// Performs the specified operation on the shards that are spanned by this session
        /// and aggregates the results from each shard into a single result.
        /// </summary>
        /// <typeparam name="T">Return value type.</typeparam>
        /// <param name="operation">The operation to be performed on each shard.</param>
        /// <param name="exitStrategy">Strategy for collection and aggregation of
        /// operation results from the shards.</param>
        /// <returns>The aggregated operation result.</returns>
        public T Execute<T>(IShardOperation<T> operation, IExitStrategy<T> exitStrategy)
        {
            return this.shardStrategy.ShardAccessStrategy.Apply(this.shards, operation, exitStrategy);
        }

        /// <summary>
        /// Force the <c>ISession</c> to flush.
        /// </summary>
        /// <remarks>
        /// Must be called at the end of a unit of work, before commiting the transaction and closing
        /// the session (<c>Transaction.Commit()</c> calls this method). <i>Flushing</i> if the process
        /// of synchronising the underlying persistent store with persistable state held in memory.
        /// </remarks>
        public void Flush()
        {
            foreach (var session in this.establishedSessionsByShard.Values)
            {
                session.Flush();
            }
        }

        /// <summary>
        /// Determines at which points Hibernate automatically flushes the session.
        /// </summary>
        /// <remarks>
        /// For a readonly session, it is reasonable to set the flush mode to <c>FlushMode.Never</c>
        /// at the start of the session (in order to achieve some extra performance).
        /// </remarks>
        public FlushMode FlushMode
        {
            get { return AnySession.FlushMode; }
            set { AddEstablishAction(s => s.FlushMode = value); }
        }

        /// <summary> 
        /// The current cache mode. 
        /// </summary>
        /// <remarks>
        /// Cache mode determines the manner in which this session can interact with
        /// the second level cache.
        /// </remarks>
        public CacheMode CacheMode
        {
            get { return AnySession.CacheMode; }
            set { AddEstablishAction(s => s.CacheMode = value); }
        }

        /// <summary>
        /// Get the <see cref="ISessionFactory" /> that created this instance.
        /// </summary>
        public ISessionFactory SessionFactory
        {
            get { return shardedSessionFactory; }
        }

        /// <summary>
        /// Deprecated.
        /// </summary>
        public IDbConnection Connection
        {
            get { throw new InvalidOperationException("On Shards this is deprecated"); }
        }

        /// <summary>
        /// Disconnect the <c>ISession</c> from the current ADO.NET connection.
        /// </summary>
        /// <remarks>
        /// If the connection was obtained by Hibernate, close it or return it to the connection
        /// pool. Otherwise return it to the application. This is used by applications which require
        /// long transactions.
        /// </remarks>
        /// <returns>The connection provided by the application or <see langword="null" /></returns>
        public IDbConnection Disconnect()
        {
            foreach (var session in establishedSessionsByShard.Values)
            {
                session.Disconnect();
            }

            // We do not allow application-supplied connections, so we can always return null
            return null;
        }

        /// <summary>
        /// Obtain a new ADO.NET connection.
        /// </summary>
        /// <remarks>
        /// This is used by applications which require long transactions
        /// </remarks>
        public void Reconnect()
        {
            foreach (var session in establishedSessionsByShard.Values)
            {
                session.Reconnect();
            }
        }

        /// <summary>
        /// Reconnect to the given ADO.NET connection.
        /// </summary>
        /// <remarks>This is used by applications which require long transactions</remarks>
        /// <param name="connection">An ADO.NET connection</param>
        public void Reconnect(IDbConnection connection)
        {
            throw new NotSupportedException("Cannot reconnect a sharded session");
        }

        /// <summary>
        /// End the <c>ISession</c> by disconnecting from the ADO.NET connection and cleaning up.
        /// </summary>
        /// <remarks>
        /// It is not strictly necessary to <c>Close()</c> the <c>ISession</c> but you must
        /// at least <c>Disconnect()</c> it.
        /// </remarks>
        /// <returns>The connection provided by the application or <see langword="null" /></returns>
        public IDbConnection Close()
        {
            Exception firstException = null;

            if (childSessionsByEntityMode != null)
            {
                // Close any child sessions. Child session will remove themselves from childSessionsByEntityMode.
                foreach (var childSession in childSessionsByEntityMode.Values.ToArray())
                {
                    childSession.Close();
                }
            }

            if (rootSession != null)
            {
                rootSession.childSessionsByEntityMode.Remove(this.entityMode);
            }

            foreach (var session in establishedSessionsByShard.Values)
            {
                try
                {
                    session.Close();
                }
                catch (Exception e)
                {
                    // We're going to try and close everything that was opened
                    if (firstException == null)
                    {
                        firstException = e;
                    }
                }
            }

            establishActions.Clear();
            establishedSessionsByShard.Clear();
            shards.Clear();
            shardsById.Clear();

            classesWithoutTopLevelSaveSupport.Clear();
            closed = true;

            if (firstException != null) throw firstException;
            return null;
        }

        /// <summary>
        /// Cancel execution of the current query.
        /// </summary>
        /// <remarks>
        /// May be called from one thread to stop execution of a query in another thread.
        /// Use with care!
        /// </remarks>
        public void CancelQuery()
        {
            // cancel across all shards
            foreach (var session in establishedSessionsByShard.Values)
            {
                session.CancelQuery();
            }
        }

        /// <summary>
        /// Is there any shard with an open session?
        /// </summary>
        public bool IsOpen
        {
            get { return !closed || establishedSessionsByShard.Values.Any(s => s.IsOpen); }
        }

        /// <summary>
        /// Is there any shard with a connected session?
        /// </summary>
        public bool IsConnected
        {
            get { return establishedSessionsByShard.Values.Any(s => s.IsConnected); }
        }

        /// <summary>
        /// Does this <c>ISession</c> contain any changes which must be
        /// synchronized with the database? Would any SQL be executed if
        /// we flushed this session?
        /// </summary>
        public bool IsDirty()
        {
            foreach (var session in establishedSessionsByShard.Values)
            {
                if (session.IsDirty()) return true;
            }
            return false;
        }

        /// <summary>
        /// Return the identifier of an entity instance cached by the <c>ISession</c>
        /// </summary>
        /// <remarks>
        /// Throws an exception if the instance is transient or associated with a different
        /// <c>ISession</c>
        /// </remarks>
        /// <param name="obj">a persistent instance</param>
        /// <returns>the identifier</returns>
        public object GetIdentifier(object obj)
        {
            foreach (var session in establishedSessionsByShard.Values)
            {
                if (session.Contains(obj))
                {
                    return session.GetIdentifier(obj);
                }
            }

            throw new TransientObjectException("Instance is transient or associated with a different Session");
        }

        /// <summary>
        /// Is this instance associated with this Session?
        /// </summary>
        /// <param name="obj">an instance of a persistent class</param>
        /// <returns>true if the given instance is associated with this Session</returns>
        public bool Contains(object obj)
        {
            foreach (var session in establishedSessionsByShard.Values)
            {
                if (session.Contains(obj)) return true;
            }
            return false;
        }

        /// <summary>
        /// Remove this instance from the session cache.
        /// </summary>
        /// <remarks>
        /// Changes to the instance will not be synchronized with the database.
        /// This operation cascades to associated instances if the association is mapped
        /// with <c>cascade="all"</c> or <c>cascade="all-delete-orphan"</c>.
        /// </remarks>
        /// <param name="obj">a persistent instance</param>
        public void Evict(object obj)
        {
            foreach (var session in establishedSessionsByShard.Values)
            {
                session.Evict(obj);
            }
        }

        #region Load

        /// <summary>
        /// Return the persistent instance of the given entity class with the given identifier,
        /// obtaining the specified lock mode.
        /// </summary>
        /// <param name="clazz">A persistent class</param>
        /// <param name="id">A valid identifier of an existing persistent instance of the class</param>
        /// <param name="lockMode">The lock level</param>
        /// <returns>the persistent instance</returns>
        public object Load(System.Type clazz, object id, LockMode lockMode)
        {
            return Load(new ShardedEntityKey(clazz, id), null);
        }

        /// <summary>
        /// Return the persistent instance of the given entity class with the given identifier,
        /// obtaining the specified lock mode, assuming the instance exists.
        /// </summary>
        /// <param name="entityName">The entity-name of a persistent class</param>
        /// <param name="id">a valid identifier of an existing persistent instance of the class</param>
        /// <param name="lockMode">the lock level</param>
        /// <returns>the persistent instance or proxy</returns>
        public object Load(string entityName, object id, LockMode lockMode)
        {
            return Load(new ShardedEntityKey(entityName, id), lockMode);
        }

        /// <summary>
        /// Return the persistent instance of the given entity class with the given identifier,
        /// assuming that the instance exists.
        /// </summary>
        /// <remarks>
        /// You should not use this method to determine if an instance exists (use a query or
        /// <see cref="ISession.Get(Type,object)" /> instead). Use this only to retrieve an instance
        /// that you assume exists, where non-existence would be an actual error.
        /// </remarks>
        /// <param name="clazz">A persistent class</param>
        /// <param name="id">A valid identifier of an existing persistent instance of the class</param>
        /// <returns>The persistent instance or proxy</returns>
        public object Load(System.Type clazz, object id)
        {
            return Load(new ShardedEntityKey(clazz, id), null);
        }

        /// <summary>
        /// Return the persistent instance of the given entity class with the given identifier,
        /// obtaining the specified lock mode.
        /// </summary>
        /// <typeparam name="T">A persistent class</typeparam>
        /// <param name="id">A valid identifier of an existing persistent instance of the class</param>
        /// <param name="lockMode">The lock level</param>
        /// <returns>the persistent instance</returns>
        public T Load<T>(object id, LockMode lockMode)
        {
            return (T)Load(new ShardedEntityKey(typeof(T), id), lockMode);
        }

        /// <summary>
        /// Return the persistent instance of the given entity class with the given identifier,
        /// assuming that the instance exists.
        /// </summary>
        /// <remarks>
        /// You should not use this method to determine if an instance exists (use a query or
        /// <see cref="ISession.Get{T}(object)" /> instead). Use this only to retrieve an instance that you
        /// assume exists, where non-existence would be an actual error.
        /// </remarks>
        /// <typeparam name="T">A persistent class</typeparam>
        /// <param name="id">A valid identifier of an existing persistent instance of the class</param>
        /// <returns>The persistent instance or proxy</returns>
        public T Load<T>(object id)
        {
            return (T)Load(new ShardedEntityKey(typeof(T), id), null);
        }

        /// <summary>
        /// Return the persistent instance of the given <paramref name="entityName"/> with the given identifier,
        /// assuming that the instance exists.
        /// </summary>
        /// <param name="entityName">The entity-name of a persistent class</param>
        /// <param name="id">a valid identifier of an existing persistent instance of the class</param>
        /// <returns>The persistent instance or proxy</returns>
        /// <remarks>
        /// You should not use this method to determine if an instance exists (use <see cref="M:NHibernate.ISession.Get(System.String,System.Object)"/>
        /// instead). Use this only to retrieve an instance that you assume exists, where non-existence
        /// would be an actual error.
        /// </remarks>
        public object Load(string entityName, object id)
        {
            return Load(new ShardedEntityKey(entityName, id), null);
        }

        private object Load(ShardedEntityKey key, LockMode lockMode)
        {
            IShard shard;
            if (TryResolveToSingleShard(key, out shard))
            {
                return shard.EstablishSession().Load(key.EntityName, key.Id, lockMode);
            }

            IUniqueResult<object> persistent;
            if (!TryGet(key, lockMode, out persistent))
            {
                this.shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(key.EntityName, key.Id);
            }
            return persistent.Value;
        }

        /// <summary>
        /// Read the persistent state associated with the given identifier into the given transient 
        /// instance.
        /// </summary>
        /// <param name="obj">An "empty" instance of the persistent class</param>
        /// <param name="id">A valid identifier of an existing persistent instance of the class</param>
        public void Load(object obj, object id)
        {
            Load(obj, CreateKey(obj, id));
        }


        private void Load(object entity, ShardedEntityKey key)
        {
            IShard shard;
            if (TryResolveToSingleShard(key, out shard))
            {
                shard.EstablishSession().Load(entity, key);
                return;
            }

            IUniqueResult<object> persistent;
            if (TryGet(key, out persistent))
            {
                Evict(persistent.Value);
                persistent.Shard.EstablishSession().Load(entity, key);
            }

            shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(key.EntityName, key);
        }

        #endregion

        #region Replicate

        /// <summary>
        /// Persist all reachable transient objects, reusing the current identifier
        /// values. Note that this will not trigger the Interceptor of the Session.
        /// </summary>
        /// <param name="obj">a detached instance of a persistent class</param>
        /// <param name="replicationMode"></param>
        public void Replicate(object obj, ReplicationMode replicationMode)
        {
            Replicate(obj, ExtractKey(null, obj), null);
        }

        /// <summary>
        /// Persist the state of the given detached instance, reusing the current
        /// identifier value.  This operation cascades to associated instances if
        /// the association is mapped with <tt>cascade="replicate"</tt>.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="obj">a detached instance of a persistent class</param>
        /// <param name="replicationMode"></param>
        public void Replicate(string entityName, object obj, ReplicationMode replicationMode)
        {
            Replicate(obj, ExtractKey(entityName, obj), replicationMode);
        }

        private void Replicate(object entity, ShardedEntityKey key, ReplicationMode replicationMode)
        {
            ShardId shardId;
            if (TryResolveToSingleShardId(key, out shardId))
            {
                currentSubgraphShardId = shardId;
                shardsById[shardId].EstablishSession().Replicate(key.EntityName, entity, replicationMode);
                return;
            }

            IUniqueResult<object> persistent;
            if (TryGet(key, out persistent))
            {
                Evict(persistent.Value);
                persistent.Shard.EstablishSession().Replicate(key.EntityName, entity, replicationMode);
            }
            else
            {
                currentSubgraphShardId = shardId = SelectShardIdForNewEntity(key.EntityName, entity);
                shardsById[shardId].EstablishSession().Replicate(key.EntityName, entity, replicationMode);
            }
        }

        #endregion

        private ShardedEntityKey CreateKey(object entity, object id)
        {
            Preconditions.CheckNotNull(entity);
            return new ShardedEntityKey(GuessEntityName(entity), id);
        }

        private ShardedEntityKey CreateKey(string entityName, object entity)
        {
            Preconditions.CheckNotNull(entity);
            return new ShardedEntityKey(
                entityName ?? GuessEntityName(entity),
                GetIdentifier(entity));
        }

        private ShardedEntityKey ExtractKey(string entityName, object entity)
        {
            Preconditions.CheckNotNull(entity);
            if (entityName == null)
            {
                entityName = GuessEntityName(entity);
            }

            var classMetadata = shardedSessionFactory.GetClassMetadata(entityName);
            var id = classMetadata.GetIdentifier(entity, this.entityMode);
            return new ShardedEntityKey(entityName, id);
        }

        #region Save

        /// <summary>
        /// Persist the given transient instance, first assigning a generated identifier.
        /// </summary>
        /// <param name="obj">A transient instance of a persistent class</param>
        /// <returns>The generated identifier</returns>
        /// <remarks>
        /// Save will use the current value of the identifier property if the <c>Assigned</c>
        /// generator is used.
        /// </remarks>
        public object Save(object obj)
        {
            return Save(null, obj);
        }

        /// <summary>
        /// Persist the given transient instance, first assigning a generated identifier. (Or
        /// using the current value of the identifier property if the <tt>assigned</tt>
        /// generator is used.)
        /// </summary>
        /// <param name="entityName">The Entity name.</param>
        /// <param name="obj">a transient instance of a persistent class</param>
        /// <returns>the generated identifier</returns>
        /// <remarks>
        /// This operation cascades to associated instances if the
        /// association is mapped with <tt>cascade="save-update"</tt>.
        /// </remarks>
        public object Save(string entityName, object obj)
        {
            Preconditions.CheckNotNull(obj);
            if (entityName == null) entityName = GuessEntityName(obj);

            ShardId shardId;
            if (!TryGetShardIdForAttachedObject(entityName, obj, out shardId))
            {
                // Detached entity
                shardId = SelectShardIdForNewEntity(entityName, obj);
                Preconditions.CheckNotNull(shardId);
            }

            currentSubgraphShardId = shardId;
            Log.Debug(String.Format("Saving object of type '{0}' to shard {1}", entityName, shardId));
            return this.shardsById[shardId].EstablishSession().Save(entityName, obj);
        }

        /// <summary>
        /// Persist the given transient instance, using the given identifier.
        /// </summary>
        /// <param name="obj">A transient instance of a persistent class</param>
        /// <param name="id">An unused valid identifier</param>
        public void Save(object obj, object id)
        {
            throw new NotSupportedException();
        }

        private ShardId SelectShardIdForNewEntity(string entityName, object entity)
        {
            Preconditions.CheckNotNull(entityName);
            Preconditions.CheckNotNull(entity);

            if (lockedShardId != null) return lockedShardId;

            /*
             * Someone is trying to save this object, and that's wonderful, but if
             * this object references or is referenced by any other objects that have already been
             * associated with a session it's important that this object end up
             * associated with the same session.  In order to make sure that happens,
             * we're going to look at the metadata for this object and see what
             * references we have, and then use those to determine the proper shard.
             * If we can't find any references we'll leave it up to the shard selection
             * strategy.
             */
            ShardId shardId;
            if (!TryGetShardIdOfAssociatedObject(entityName, entity, out shardId))
            {
                CheckForUnsupportedToplevelSave(entity.GetType());
                shardId = this.shardStrategy.ShardSelectionStrategy.SelectShardIdForNewObject(entity);
            }

            // lock has been requested but shard has not yet been selected - lock it in
            if (lockedShard) lockedShardId = shardId;

            Log.Debug(string.Format("Selected shard '{0}' for object of type '{1}'", shardId.Id, entityName));
            return shardId;
        }

        /*
         * We already know that we don't have a shardId locked in for this session,
         * and we already know that this object can't grab its session from some
         * other object (we looked).  If this class is in the set of classes
         * that don't support top-level saves, it's an error.
         * This is to prevent clients from accidentally splitting their object graphs
         * across multiple shards.
         */
        private void CheckForUnsupportedToplevelSave(System.Type entityClass)
        {
            if (classesWithoutTopLevelSaveSupport.Contains(entityClass))
            {
                string msg = string.Format("Attempt to save object of type {0} as top-level object", entityClass.Name);
                Log.Error(msg);
                throw new HibernateException(msg);
            }
        }

        private bool TryGetShardIdOfAssociatedObject(string entityName, object entity, out ShardId result)
        {
            Preconditions.CheckNotNull(entityName);
            Preconditions.CheckNotNull(entity);

            using (var shardIdEnum = GetAssociatedShardIds(entityName, entity).GetEnumerator())
            {
                if (!shardIdEnum.MoveNext())
                {
                    result = null;
                    return false;
                }

                result = shardIdEnum.Current.Value;
                if (checkAllAssociatedObjectsForDifferentShards)
                {
                    while (shardIdEnum.MoveNext())
                    {
                        ThrowIfConflicitingShardId(entityName, result, shardIdEnum.Current);
                    }
                }
            }

            return true;
        }

        /**
         * TODO(maxr) I can see this method benefitting from a cache that lets us quickly
         * see which properties we might need to look at.
         */
        private IEnumerable<KeyValuePair<string, ShardId>> GetAssociatedShardIds(string entityName, object entity)
        {
            Preconditions.CheckNotNull(entityName);
            Preconditions.CheckNotNull(entity);

            string associatedEntityName;
            ShardId associatedShardId;
            Dictionary<IAssociationType, ICollection> collectionAssociations = null;

            IClassMetadata classMetaData = shardedSessionFactory.ControlFactory.GetClassMetadata(entityName);
            foreach (var pair in TypeUtil.GetAssociations(classMetaData, entity, this.entityMode))
            {
                if (pair.Key.IsCollectionType)
                {
                    /**
                     * collection types are more expensive to evaluate (might involve
                     * lazy-loading the contents of the collection from the db), so
                     * let's hold off until the end on the chance that we can fail
                     * quickly.
                     */
                    var collection = pair.Value as ICollection;
                    if (collection != null)
                    {
                        if (collectionAssociations == null)
                        {
                            collectionAssociations = new Dictionary<IAssociationType, ICollection>();
                        }
                        collectionAssociations.Add(pair.Key, collection);
                    }
                }
                else
                {
                    associatedEntityName = pair.Key.GetAssociatedEntityName(shardedSessionFactory.ControlFactory);
                    if (TryGetShardIdForAttachedObject(associatedEntityName, pair.Value, out associatedShardId))
                    {
                        yield return new KeyValuePair<string, ShardId>(associatedEntityName, associatedShardId);
                    }
                }
            }

            if (collectionAssociations != null)
            {
                foreach (var collectionAssociation in collectionAssociations)
                {
                    associatedEntityName = collectionAssociation.Key.GetAssociatedEntityName(
                        this.shardedSessionFactory.ControlFactory);
                    foreach (var item in collectionAssociation.Value)
                    {
                        if (TryGetShardIdForAttachedObject(associatedEntityName, item, out associatedShardId))
                        {
                            yield return new KeyValuePair<string, ShardId>(associatedEntityName, associatedShardId);
                        }
                    }
                }
            }
        }

        private static void ThrowIfConflicitingShardId(string entityName, ShardId shardId, KeyValuePair<string, ShardId> associatedShardId)
        {
            if (!associatedShardId.Value.Equals(shardId))
            {
                string message = string.Format(
                    "Object of entity type '{0}' is on shard '{1}' but an associated object of type '{2}' is on shard '{3}'.",
                    entityName, shardId, associatedShardId.Key, associatedShardId.Value);
                Log.Error(message);
                throw new CrossShardAssociationException(message);
            }
        }

        #endregion

        #region SaveOrUpdate

        /// <summary>
        /// Either <c>Save()</c> or <c>Update()</c> the given instance, depending upon the value of
        /// its identifier property.
        /// </summary>
        /// <param name="obj">A transient instance containing new or updated state</param>
        /// <remarks>
        /// By default the instance is always saved. This behaviour may be adjusted by specifying
        /// an <c>unsaved-value</c> attribute of the identifier property mapping
        /// </remarks>
        public void SaveOrUpdate(object obj)
        {
            SaveOrUpdate(null, obj);
        }

        /// <summary>
        /// Either <see cref="M:NHibernate.ISession.Save(System.String,System.Object)"/> or <see cref="M:NHibernate.ISession.Update(System.String,System.Object)"/>
        /// the given instance, depending upon resolution of the unsaved-value checks
        /// (see the manual for discussion of unsaved-value checking).
        /// </summary>
        /// <param name="entityName">The name of the entity</param>
        /// <param name="obj">a transient or detached instance containing new or updated state</param>
        /// <seealso cref="M:NHibernate.ISession.Save(System.String,System.Object)"/>
        /// <seealso cref="M:NHibernate.ISession.Update(System.String,System.Object)"/>
        /// <remarks>
        /// This operation cascades to associated instances if the association is mapped
        /// with <tt>cascade="save-update"</tt>.
        /// </remarks>
        public void SaveOrUpdate(string entityName, object obj)
        {
            IShard shard;
            if (TryGetShardForAttachedEntity(obj, out shard))
            {
                // attached object
                shard.EstablishSession().SaveOrUpdate(entityName, obj);
                return;
            }

            var key = ExtractKey(entityName, obj);
            if (!key.IsNull)
            {
                // detached object
                if (TryResolveToSingleShard(key, out shard))
                {
                    shard.EstablishSession().SaveOrUpdate(entityName, obj);
                    return;
                }

                /**
                 * Too bad, we've got a detached object that could be on more than 1 shard.
                 * The only safe way to handle this is to try and lookup the object, and if
                 * it exists, do a merge, and if it doesn't, do a save.
                 */
                IUniqueResult<object> persistent;
                if (TryGet(key, out persistent))
                {
                    persistent.Shard.EstablishSession().Merge(entityName, obj);
                    return;
                }
            }

            shard.EstablishSession().Save(entityName, obj);
        }

        #endregion

        #region Update

        /// <summary>
        /// Update the persistent instance with the identifier of the given transient instance.
        /// </summary>
        /// <remarks>
        /// If there is a persistent instance with the same identifier, an exception is thrown. If
        /// the given transient instance has a <see langword="null" /> identifier, an exception will be thrown.
        /// </remarks>
        /// <param name="obj">A transient instance containing updated state</param>
        public void Update(object obj)
        {
            Update(null, obj);
        }

        /// <summary>
        /// Update the persistent state associated with the given identifier.
        /// </summary>
        /// <remarks>
        /// An exception is thrown if there is a persistent instance with the same identifier
        /// in the current session.
        /// </remarks>
        /// <param name="obj">A transient instance containing updated state</param>
        /// <param name="id">Identifier of persistent instance</param>
        public void Update(object obj, object id)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Update the persistent instance with the identifier of the given detached
        /// instance.
        /// </summary>
        /// <param name="entityName">The Entity name.</param>
        /// <param name="obj">a detached instance containing updated state</param>
        /// <remarks>
        /// If there is a persistent instance with the same identifier,
        /// an exception is thrown. This operation cascades to associated instances
        /// if the association is mapped with <tt>cascade="save-update"</tt>.
        /// </remarks>
        public void Update(string entityName, object obj)
        {
            IShard shard;
            if (TryGetShardForAttachedEntity(obj, out shard))
            {
                // attached object
                shard.EstablishSession().Update(entityName, obj);
                return;
            }

            var key = ExtractKey(entityName, obj);
            if (!key.IsNull)
            {
                // detached object
                if (TryResolveToSingleShard(key, out shard))
                {
                    shard.EstablishSession().Update(entityName, obj);
                    return;
                }

                /**
                  * Too bad, we've got a detached object that could be on more than 1 shard.
                  * The only safe way to perform the update is to load the object and then
                  * do a merge.
                  */
                IUniqueResult<object> persistent;
                if (TryGet(key, out persistent))
                {
                    persistent.Shard.EstablishSession().Merge(entityName, obj);
                    return;
                }
            }

            /**
             * This is an error condition.  In order to provide the same behavior
             * as a non-sharded session we're just going to dispatch the update
             * to a random shard (we know it will fail because either we don't have
             * an id or the lookup returned).
             */
            AnySession.Update(entityName, obj);
            // this call may succeed but the commit will fail
        }

        #endregion

        #region Merge

        /// <summary>
        /// Copy the state of the given object onto the persistent object with the same
        /// identifier. If there is no persistent instance currently associated with
        /// the session, it will be loaded. Return the persistent instance. If the
        /// given instance is unsaved, save a copy of and return it as a newly persistent
        /// instance. The given instance does not become associated with the session.
        /// This operation cascades to associated instances if the association is mapped
        /// with <tt>cascade="merge"</tt>.<br/>
        /// The semantics of this method are defined by JSR-220.
        /// </summary>
        /// <param name="obj">a detached instance with state to be copied</param>
        /// <returns>an updated persistent instance</returns>
        public object Merge(object obj)
        {
            return Merge(null, obj);
        }

        /// <summary>
        /// Copy the state of the given object onto the persistent object with the same
        /// identifier. If there is no persistent instance currently associated with
        /// the session, it will be loaded. Return the persistent instance. If the
        /// given instance is unsaved, save a copy of and return it as a newly persistent
        /// instance. The given instance does not become associated with the session.
        /// This operation cascades to associated instances if the association is mapped
        /// with <tt>cascade="merge"</tt>.<br/>
        /// The semantics of this method are defined by JSR-220.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="obj">a detached instance with state to be copied </param>
        /// <returns> an updated persistent instance </returns>
        public object Merge(string entityName, object obj)
        {
            var key = ExtractKey(entityName, obj);

            ShardId shardId;
            if (TryResolveToSingleShardId(key, out shardId))
            {
                currentSubgraphShardId = shardId;
                return shardsById[shardId].EstablishSession().Merge(entityName, obj);
            }

            IUniqueResult<object> persistent;
            if (TryGet(key, out persistent))
            {
                return persistent.Shard.EstablishSession().Merge(entityName, obj);
            }

            currentSubgraphShardId = shardId = SelectShardIdForNewEntity(key.EntityName, obj);
            return shardsById[shardId].EstablishSession().Merge(entityName, obj);
        }

        #endregion

        #region Persist

        /// <summary>
        /// Make a transient instance persistent. This operation cascades to associated
        /// instances if the association is mapped with <tt>cascade="persist"</tt>.<br/>
        /// The semantics of this method are defined by JSR-220.
        /// </summary>
        /// <param name="obj">a transient instance to be made persistent</param>
        public void Persist(object obj)
        {
            Persist(null, obj);
        }

        /// <summary>
        /// Make a transient instance persistent. This operation cascades to associated
        /// instances if the association is mapped with <tt>cascade="persist"</tt>.<br/>
        /// The semantics of this method are defined by JSR-220.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="obj">a transient instance to be made persistent</param>
        public void Persist(string entityName, object obj)
        {
            Preconditions.CheckNotNull(obj);
            if (entityName == null) entityName = GuessEntityName(obj);

            ShardId shardId;
            if (!TryGetShardIdForAttachedObject(entityName, obj, out shardId))
            {
                // Detached object
                shardId = SelectShardIdForNewEntity(entityName, obj);
            }

            currentSubgraphShardId = shardId;
            Log.Debug(string.Format("Persisting object of type '{0}' to shard '{1}'", entityName, shardId));
            shardsById[shardId].EstablishSession().Persist(entityName, obj);
        }

        #endregion

        #region SaveOrUpdateCopy

        /// <summary>
        /// Copy the state of the given object onto the persistent object with the same
        /// identifier. If there is no persistent instance currently associated with 
        /// the session, it will be loaded. Return the persistent instance. If the 
        /// given instance is unsaved or does not exist in the database, save it and 
        /// return it as a newly persistent instance. Otherwise, the given instance
        /// does not become associated with the session.
        /// </summary>
        /// <param name="obj">a transient instance with state to be copied</param>
        /// <returns>an updated persistent instance</returns>
        public object SaveOrUpdateCopy(object obj)
        {
            return SaveOrUpdateCopy(null, obj);
        }

        /// <summary>
        /// Copy the state of the given object onto the persistent object with the 
        /// given identifier. If there is no persistent instance currently associated 
        /// with the session, it will be loaded. Return the persistent instance. If
        /// there is no database row with the given identifier, save the given instance
        /// and return it as a newly persistent instance. Otherwise, the given instance
        /// does not become associated with the session.
        /// </summary>
        /// <param name="obj">a persistent or transient instance with state to be copied</param>
        /// <param name="id">the identifier of the instance to copy to</param>
        /// <returns>an updated persistent instance</returns>
        public object SaveOrUpdateCopy(object obj, object id)
        {
            var key = ExtractKey(null, obj);

            ShardId shardId;
            if (TryResolveToSingleShardId(key, out shardId))
            {
                // attached
                currentSubgraphShardId = shardId;
                return shardsById[shardId].EstablishSession().SaveOrUpdateCopy(obj, key.Id);
            }

            IUniqueResult<object> persistent;
            if (TryGet(key, out persistent))
            {
                // detached
                return persistent.Shard.EstablishSession().SaveOrUpdateCopy(obj, key.Id);
            }

            currentSubgraphShardId = shardId = SelectShardIdForNewEntity(key.EntityName, obj);
            return shardsById[shardId].EstablishSession().SaveOrUpdateCopy(key.EntityName, obj);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Remove a persistent instance from the datastore.
        /// </summary>
        /// <remarks>
        /// The argument may be an instance associated with the receiving <c>ISession</c> or a
        /// transient instance with an identifier associated with existing persistent state.
        /// </remarks>
        /// <param name="obj">The instance to be removed</param>
        public void Delete(object obj)
        {
            Delete(null, obj);
        }

        public void Delete(string entityName, object obj)
        {
            IShard shard;
            if (TryGetShardForAttachedEntity(obj, out shard))
            {
                // attached object
                shard.EstablishSession().Delete(entityName, obj);
                return;
            }

            /**
			 * Detached object.
			 * We can't just try to delete on each shard because if you have an
			 * object associated with Session x and you try to delete that object in
			 * Session y, and if that object has persistent collections, Hibernate will
			 * blow up because it will try to associate the persistent collection with
			 * a different Session as part of the cascade.  In order to avoid this we
			 * need to be precise about the shard on which we perform the delete.
			 *
			 * First let's see if we can derive the shard just from the object's id.
			 */
            var key = ExtractKey(entityName, obj);
            if (!key.IsNull)
            {
                if (TryResolveToSingleShard(key, out shard))
                {
                    shard.EstablishSession().Delete(entityName, obj);
                    return;
                }

                /**
                 * Too bad, we've got a detached object that could be on more than 1 shard.
                 * The only safe way to perform the delete is to load the object before
                 * deleting.
                 */
                IUniqueResult<object> persistent;
                if (TryGet(key, out persistent))
                {
                    persistent.Shard.EstablishSession().Delete(entityName, persistent.Value);
                }
            }
        }

        /// <summary>
        /// Delete all objects returned by the query.
        /// </summary>
        /// <param name="query">The query string</param>
        /// <returns>Returns the number of objects deleted.</returns>
        public int Delete(string query)
        {
            return Execute(
                new DeleteOperation(s => s.Delete(query)),
                new ExecuteUpdateExitStrategy());
        }

        /// <summary>
        /// Delete all objects returned by the query.
        /// </summary>
        /// <param name="query">The query string</param>
        /// <param name="value">A value to be written to a "?" placeholer in the query</param>
        /// <param name="type">The hibernate type of value.</param>
        /// <returns>The number of instances deleted</returns>
        public int Delete(string query, object value, IType type)
        {
            return Execute(
                new DeleteOperation(s => s.Delete(query, value, type)),
                new ExecuteUpdateExitStrategy());
        }

        /// <summary>
        /// Delete all objects returned by the query.
        /// </summary>
        /// <param name="query">The query string</param>
        /// <param name="values">A list of values to be written to "?" placeholders in the query</param>
        /// <param name="types">A list of Hibernate types of the values</param>
        /// <returns>The number of instances deleted</returns>
        public int Delete(string query, object[] values, IType[] types)
        {
            return Execute(
                new DeleteOperation(s => s.Delete(query, values, types)),
                new ExecuteUpdateExitStrategy());
        }

        private class DeleteOperation : IShardOperation<int>
        {
            private readonly Func<ISession, int> deleteAction;

            public DeleteOperation(Func<ISession, int> deleteAction)
            {
                this.deleteAction = deleteAction;
            }

            public int Execute(IShard shard)
            {
                return deleteAction(shard.EstablishSession());
            }

            public string OperationName
            {
                get { return "delete(query)"; }
            }
        }

        #endregion

        #region Lock

        /// <summary>
        /// Determine the current lock mode of the given object
        /// </summary>
        /// <param name="obj">A persistent instance</param>
        /// <returns>The current lock mode</returns>
        public LockMode GetCurrentLockMode(object obj)
        {
            return ApplyShardFuncToAttachedObject((s, o) => s.GetCurrentLockMode(o), obj);
        }

        /// <summary>
        /// Obtain the specified lock level upon the given object.
        /// </summary>
        /// <param name="obj">A persistent instance</param>
        /// <param name="lockMode">The lock level</param>
        public void Lock(object obj, LockMode lockMode)
        {
            ApplyShardActionToAttachedObject((s, o) => s.Lock(o, lockMode), obj);
        }

        public void Lock(string entityName, object obj, LockMode lockMode)
        {
            ApplyShardActionToAttachedObject((s, o) => s.Lock(entityName, o, lockMode), obj);
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Re-read the state of the given instance from the underlying database.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It is inadvisable to use this to implement long-running sessions that span many
        /// business tasks. This method is, however, useful in certain special circumstances.
        /// </para>
        /// <para>
        /// For example,
        /// <list>
        ///		<item>Where a database trigger alters the object state upon insert or update</item>
        ///		<item>After executing direct SQL (eg. a mass update) in the same session</item>
        ///		<item>After inserting a <c>Blob</c> or <c>Clob</c></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <param name="obj">A persistent instance</param>
        public void Refresh(object obj)
        {
            Refresh(obj, null);
        }

        /// <summary>
        /// Re-read the state of the given instance from the underlying database, with
        /// the given <c>LockMode</c>.
        /// </summary>
        /// <remarks>
        /// It is inadvisable to use this to implement long-running sessions that span many
        /// business tasks. This method is, however, useful in certain special circumstances.
        /// </remarks>
        /// <param name="obj">a persistent or transient instance</param>
        /// <param name="lockMode">the lock mode to use</param>
        public void Refresh(object obj, LockMode lockMode)
        {
            IShard shard;
            if (TryGetShardForAttachedEntity(obj, out shard))
            {
                // Attached object
                shard.EstablishSession().Refresh(obj, lockMode);
                return;
            }
        }

        #endregion

        #region Transaction

        /// <summary>
        /// Get the current Unit of Work and return the associated <c>ITransaction</c> object.
        /// </summary>
        public ITransaction Transaction
        {
            get
            {
                ErrorIfClosed();

                lock (this.transactionLock)
                {
                    if (this.transaction == null)
                    {
                        this.transaction = new ShardedTransactionImpl(this);
                    }
                    return transaction;
                }
            }
        }

        /// <summary>
        /// Begin a unit of work and return the associated <c>ITransaction</c> object.
        /// </summary>
        /// <remarks>
        /// If a new underlying transaction is required, begin the transaction. Otherwise
        /// continue the new work in the context of the existing underlying transaction.
        /// The class of the returned <see cref="ITransaction" /> object is determined by
        /// the property <c>transaction_factory</c>
        /// </remarks>
        /// <returns>A transaction instance</returns>
        public ITransaction BeginTransaction()
        {
            return BeginTransaction(IsolationLevel.Unspecified);
        }

        /// <summary>
        /// Begin a transaction with the specified <c>isolationLevel</c>
        /// </summary>
        /// <param name="isolationLevel">Isolation level for the new transaction</param>
        /// <returns>A transaction instance having the specified isolation level</returns>
        public ITransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            ErrorIfClosed();

            var result = Transaction;
            result.Begin(isolationLevel);
            return result;
        }

        public void AfterTransactionBegin(IShardedTransaction tx)
        {
            lock (this.transactionLock)
            {
                if (this.transaction != tx) return;
            }

            foreach (var session in this.establishedSessionsByShard.Values)
            {
                tx.Enlist(session);
            }
        }

        public void AfterTransactionCompletion(IShardedTransaction tx, bool? success)
        {
            lock (this.transactionLock)
            {
                if (this.transaction == tx)
                {
                    this.transaction = null;
                }
            }
        }

        #endregion

        #region CreateXXX, QueryOver, GetNamedXXX

        public ICriteria CreateCriteria<T>() where T : class
        {
            return CreateCriteria(typeof(T));
        }

        public ICriteria CreateCriteria<T>(string alias) where T : class
        {
            return CreateCriteria(typeof(T), alias);
        }

        /// <summary>
        /// Creates a new <c>Criteria</c> for the entity class.
        /// </summary>
        /// <param name="persistentClass">The class to Query</param>
        /// <returns>An ICriteria object</returns>
        public ICriteria CreateCriteria(System.Type persistentClass)
        {
            return new ShardedCriteriaImpl(this, s => s.CreateCriteria(persistentClass));
        }

        /// <summary>
        /// Creates a new <c>Criteria</c> for the entity class with a specific alias
        /// </summary>
        /// <param name="persistentClass">The class to Query</param>
        /// <param name="alias">The alias of the entity</param>
        /// <returns>An ICriteria object</returns>
        public ICriteria CreateCriteria(System.Type persistentClass, string alias)
        {
            return new ShardedCriteriaImpl(this, s => s.CreateCriteria(persistentClass, alias));
        }

        public ICriteria CreateCriteria(string entityName)
        {
            return new ShardedCriteriaImpl(this, s => s.CreateCriteria(entityName));
        }

        public ICriteria CreateCriteria(string entityName, string alias)
        {
            return new ShardedCriteriaImpl(this, s => s.CreateCriteria(entityName, alias));
        }

        IQueryOver<T, T> ISession.QueryOver<T>(Expression<Func<T>> alias)
        {
            throw new NotSupportedException();
        }

        IQueryOver<T, T> ISession.QueryOver<T>()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Create a new instance of <c>Query</c> for the given query string
        /// </summary>
        /// <param name="queryString">A hibernate query string</param>
        /// <returns>The query</returns>
        public IQuery CreateQuery(string queryString)
        {
            return ShardedQueryImpl.CreateQuery(this, queryString);
        }

        public IQuery CreateQuery(IQueryExpression queryExpression)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Create a new instance of <c>Query</c> for the given collection and filter string
        /// </summary>
        /// <param name="collection">A persistent collection</param>
        /// <param name="queryString">A hibernate query</param>
        /// <returns>A query</returns>
        public IQuery CreateFilter(object collection, string queryString)
        {
            var shard = GetShardForCollection(collection, shards);

            // If collection is not associated with any of our shards, we just delegate to
            // a random shard. We'll end up failing, but we'll fail with the error that users 
            // typically get.
            var session = shard == null
                ? AnySession
                : shard.EstablishSession();
            return session.CreateFilter(collection, queryString);
        }

        private IShard GetShardForCollection(object collection, IEnumerable<IShard> shardsToConsider)
        {
            foreach (IShard shard in shardsToConsider)
            {
                ISession session;
                if (this.establishedSessionsByShard.TryGetValue(shard, out session))
                {
                    var sessionImplementor = session.GetSessionImplementation();
                    if (sessionImplementor.PersistenceContext.GetCollectionEntryOrNull(collection) != null)
                    {
                        return shard;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Obtain an instance of <see cref="IQuery" /> for a named query string defined in the
        /// mapping file.
        /// </summary>
        /// <param name="queryName">The name of a query defined externally.</param>
        /// <returns>An <see cref="IQuery"/> from a named query string.</returns>
        /// <remarks>
        /// The query can be either in <c>HQL</c> or <c>SQL</c> format.
        /// </remarks>
        public IQuery GetNamedQuery(string queryName)
        {
            return ShardedQueryImpl.GetNamedQuery(this, queryName);
        }

        /// <summary>
        /// Create a new instance of <see cref="ISQLQuery" /> for the given SQL query string.
        /// </summary>
        /// <param name="queryString">a query expressed in SQL</param>
        /// <returns>An <see cref="ISQLQuery"/> from the SQL string</returns>
        public ISQLQuery CreateSQLQuery(string queryString)
        {
            return new ShardedSQLQueryImpl(this, queryString);
        }

        /// <summary>
        /// Create a multi query, a query that can send several
        /// queries to the server, and return all their results in a single
        /// call.
        /// </summary>
        /// <returns>
        /// An <see cref="IMultiQuery"/> that can return
        /// a list of all the results of all the queries.
        /// Note that each query result is itself usually a list.
        /// </returns>
        public IMultiQuery CreateMultiQuery()
        {
            return new ShardedMultiQueryImpl(this);
        }

        /// <summary>
        /// An <see cref="IMultiCriteria"/> that can return a list of all the results
        /// of all the criterias.
        /// </summary>
        /// <returns></returns>
        public IMultiCriteria CreateMultiCriteria()
        {
            return new ShardedMultiCriteriaImpl(this);
        }

        #endregion

        /// <summary>
        /// Completely clear the session. Evict all loaded instances and cancel all pending
        /// saves, updates and deletions. Do not close open enumerables or instances of
        /// <c>ScrollableResults</c>.
        /// </summary>
        public void Clear()
        {
            foreach (var session in this.establishedSessionsByShard.Values)
            {
                session.Clear();
            }
        }

        #region Get

        /// <summary>
        /// Return the persistent instance of the given entity class with the given identifier, or null
        /// if there is no such persistent instance. (If the instance, or a proxy for the instance, is
        /// already associated with the session, return that instance or proxy.)
        /// </summary>
        /// <param name="clazz">a persistent class</param>
        /// <param name="id">an identifier</param>
        /// <returns>a persistent instance or null</returns>
        public object Get(System.Type clazz, object id)
        {
            return Get(new ShardedEntityKey(clazz, id), null).Value;
        }

        public object Get(string entityName, object id)
        {
            return Get(new ShardedEntityKey(entityName, id), null).Value;
        }

        /// <summary>
        /// Return the persistent instance of the given entity class with the given identifier, or null
        /// if there is no such persistent instance. Obtain the specified lock mode if the instance
        /// exists.
        /// </summary>
        /// <param name="clazz">a persistent class</param>
        /// <param name="id">an identifier</param>
        /// <param name="lockMode">the lock mode</param>
        /// <returns>a persistent instance or null</returns>
        public object Get(System.Type clazz, object id, LockMode lockMode)
        {
            return Get(new ShardedEntityKey(clazz, id), lockMode).Value;
        }

        /// <summary>
        /// Strongly-typed version of <see cref="ISession.Get(Type,object)" />
        /// </summary>
        public T Get<T>(object id)
        {
            return (T)Get(new ShardedEntityKey(typeof(T), id), null).Value;
        }

        /// <summary>
        /// Strongly-typed version of <see cref="ISession.Get(Type,object,LockMode)" />
        /// </summary>
        public T Get<T>(object id, LockMode lockMode)
        {
            return (T)Get(new ShardedEntityKey(typeof(T), id), lockMode).Value;
        }

        private IUniqueResult<object> Get(ShardedEntityKey key, LockMode mode)
        {
            // we're not letting people customize shard selection by lockMode
            var shardOperation = new GetShardOperation(key, mode);
            var exitStrategy = new UniqueResultExitStrategy<object>();
            this.shardStrategy.ShardAccessStrategy.Apply(ResolveToShards(key), shardOperation, exitStrategy);
            return exitStrategy;
        }

        private bool TryGet(ShardedEntityKey key, out IUniqueResult<object> result)
        {
            result = key.IsNull ? null : Get(key, null);
            return result != null;
        }

        private bool TryGet(ShardedEntityKey key, LockMode lockMode, out IUniqueResult<object> result)
        {
            result = key.IsNull ? null : Get(key, lockMode);
            return result != null;
        }

        private class GetShardOperation : IShardOperation<object>
        {
            private readonly ShardedEntityKey key;
            private readonly LockMode lockMode;

            public GetShardOperation(ShardedEntityKey key, LockMode lockMode)
            {
                this.key = key;
                this.lockMode = lockMode;
            }

            public object Execute(IShard shard)
            {
                var session = shard.EstablishSession();
                // TODO: NHibernate seems to miss an ISession.Get(string entityName, object id, LockMode lockMode) overload.
                if (this.lockMode == null)
                {
                    return session.Get(this.key.EntityName, this.key.Id);
                }

                try
                {
                    return session.Load(this.key.EntityName, this.key.Id, this.lockMode);
                }
                catch (ObjectNotFoundException)
                {
                    return null;
                }
            }

            public string OperationName
            {
                get { return "get(entityName, id, lockMode)"; }
            }
        }

        #endregion

        /// <summary> 
        /// Return the entity name for a persistent entity
        /// </summary>
        /// <param name="obj">a persistent entity</param>
        /// <returns> the entity name </returns>
        public string GetEntityName(object obj)
        {
            return ApplyShardFuncToAttachedObject((s, o) => s.GetEntityName(o), obj);
        }

        /**
          * Helper method we can use when we need to find the Shard with which a
          * specified object is associated and invoke the method on that Shard.
          * If the object isn't associated with a Session we just invoke it on a
          * random Session with the expectation that this will cause an error.
          */
        private T ApplyShardFuncToAttachedObject<T>(Func<ISession, object, T> action, object obj)
        {
            IShard shard;
            if (TryGetShardForAttachedEntity(obj, out shard))
            {
                return action(shard.EstablishSession(), obj);
            }

            return action(AnySession, obj);
        }

        private void ApplyShardActionToAttachedObject(Action<ISession, object> action, object entity)
        {
            IShard shard;
            if (TryGetShardForAttachedEntity(entity, out shard))
            {
                action(shard.EstablishSession(), entity);
                return;
            }

            action(AnySession, entity);
        }

        #region Filter

        /// <summary>
        /// Enable the named filter for this current session.
        /// </summary>
        /// <param name="filterName">The name of the filter to be enabled.</param>
        /// <returns>The Filter instance representing the enabled fiter.</returns>
        public IFilter EnableFilter(string filterName)
        {
            if (this.enabledFilters == null)
            {
                this.enabledFilters = new Dictionary<string, IShardedFilter>();
                this.establishActions.Add(s =>
                {
                    foreach (var f in this.enabledFilters.Values)
                    {
                        f.EnableFor(s);
                    }
                });
            }

            var result = new ShardedFilterImpl(this, filterName);
            foreach (var session in this.establishedSessionsByShard.Values)
            {
                result.EnableFor(session);
            }

            this.enabledFilters[filterName] = result;
            return result;
        }

        /// <summary>
        /// Retrieve a currently enabled filter by name.
        /// </summary>
        /// <param name="filterName">The name of the filter to be retrieved.</param>
        /// <returns>The Filter instance representing the enabled fiter.</returns>
        public IFilter GetEnabledFilter(string filterName)
        {
            IShardedFilter result;
            return (this.enabledFilters != null && this.enabledFilters.TryGetValue(filterName, out result))
                ? result
                : null;
        }

        /// <summary>
        /// Disable the named filter for the current session.
        /// </summary>
        /// <param name="filterName">The name of the filter to be disabled.</param>
        public void DisableFilter(string filterName)
        {
            IShardedFilter filter;
            if (this.enabledFilters != null && this.enabledFilters.TryGetValue(filterName, out filter))
            {
                this.enabledFilters.Remove(filterName);
                filter.Disable();
            }
        }

        #endregion

        /// <summary>
        /// Sets the batch size of the session
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public ISession SetBatchSize(int batchSize)
        {
            AddEstablishAction(s => s.SetBatchSize(batchSize));
            return this;
        }

        /// <summary>
        /// Gets the session implementation.
        /// </summary>
        /// <remarks>
        /// This method is provided in order to get the <b>NHibernate</b> implementation of the session from wrapper implementions.
        /// Implementors of the <seealso cref="ISession"/> interface should return the NHibernate implementation of this method.
        /// </remarks>
        /// <returns>
        /// An NHibernate implementation of the <seealso cref="ISessionImplementor"/> interface 
        /// </returns>
        public ISessionImplementor GetSessionImplementation()
        {
            throw new NotSupportedException();
        }

        public ISession GetSession(EntityMode entityMode)
        {
            if (childSessionsByEntityMode == null)
            {
                childSessionsByEntityMode = new Dictionary<EntityMode, IShardedSession>();
            }

            IShardedSession result;
            if (!childSessionsByEntityMode.TryGetValue(entityMode, out result))
            {
                result = new ShardedSessionImpl(this, entityMode);
                childSessionsByEntityMode.Add(entityMode, result);
            }
            return result;
        }

        public EntityMode ActiveEntityMode
        {
            get { return this.entityMode; }
        }

        /// <summary> Get the statistics for this session.</summary>
        public ISessionStatistics Statistics
        {
            get
            {
                if (this.statistics == null)
                {
                    this.statistics = new ShardedSessionStatistics();
                    AddEstablishAction(s => statistics.CollectFor(s));
                }
                return this.statistics;
            }
        }

        ///<summary>
        ///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///</summary>
        ///<filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (!closed)
            {
                Log.Warn("ShardedSessionImpl is being garbage collected but it was never properly closed.");
                try
                {
                    Close();
                }
                catch (Exception e)
                {
                    Log.Warn("Caught exception trying to close.", e);
                }
            }
        }

        #endregion

        #region Private methods

        private string GuessEntityName(object entity)
        {
            var proxy = entity as INHibernateProxy;
            if (proxy != null)
            {
                var initializer = proxy.HibernateLazyInitializer;
                entity = initializer.GetImplementation();
            }

            string entityName = this.interceptor != null
                ? this.interceptor.GetEntityName(entity)
                : null;
            if (entityName == null)
            {
                System.Type entityType = entity.GetType();
                entityName = this.shardedSessionFactory.TryGetGuessEntityName(entityType) ?? entityType.FullName;
            }
            return entityName;
        }

        private bool TryResolveToSingleShard(ShardedEntityKey key, out IShard result)
        {
            ShardId shardId;
            if (TryResolveToSingleShardId(key, out shardId))
            {
                result = this.shardsById[shardId];
                return true;
            }

            result = null;
            return false;
        }

        private IEnumerable<IShard> ResolveToShards(ShardedEntityKey key)
        {
            IShard firstShard = null;
            HashSet<IShard> shards = null;

            foreach (var shardId in ResolveToShardIds(key))
            {
                var shard = this.shardsById[shardId];
                if (firstShard == null)
                {
                    firstShard = shard;
                }
                else if (shards != null)
                {
                    shards.Add(shard);
                }
                else if (shard != firstShard)
                {
                    shards = new HashSet<IShard> { firstShard, shard };
                }
            }

            if (shards != null) return shards;
            if (firstShard != null) return new[] { firstShard };
            return Enumerable.Empty<IShard>();
        }

        private bool TryResolveToSingleShardId(ShardedEntityKey key, out ShardId result)
        {
            return TryResolveToSingleShardId(ResolveToShardIds(key), out result);
        }

        private static bool TryResolveToSingleShardId(IEnumerable<ShardId> shardIds, out ShardId result)
        {
            using (var shardIdEnum = shardIds.GetEnumerator())
            {
                if (shardIdEnum.MoveNext())
                {
                    result = shardIdEnum.Current;
                    if (!shardIdEnum.MoveNext()) return true;
                }
            }

            result = null;
            return false;
        }

        private IEnumerable<ShardId> ResolveToShardIds(ShardedEntityKey key)
        {
            ShardId singleShardId;
            if (!key.IsNull && this.shardedSessionFactory.TryExtractShardIdFromKey(key, out singleShardId))
            {
                yield return singleShardId;
            }
            else
            {
                foreach (var shardId in this.shardStrategy.ShardResolutionStrategy.ResolveShardIds(key))
                {
                    yield return shardId;
                }
            }
        }

        private bool TryGetShardForAttachedEntity(object entity, out IShard result)
        {
            foreach (var pair in this.establishedSessionsByShard)
            {
                if (pair.Value.Contains(entity))
                {
                    result = pair.Key;
                    return true;
                }
            }

            result = null;
            return false;
        }

        void ErrorIfClosed()
        {
            if (closed)
            {
                throw new SessionException("Session is closed!");
            }
        }

        #endregion

        #region Inner classes

        private class CrossShardRelationshipDetector : EmptyInterceptor
        {
            private readonly ShardedSessionImpl shardedSession;

            public CrossShardRelationshipDetector(ShardedSessionImpl shardedSession)
            {
                this.shardedSession = shardedSession;
            }

            public override bool OnFlushDirty(
                object entity,
                object id,
                object[] currentState,
                object[] previousState,
                string[] propertyNames,
                IType[] types)
            {
                var expectedShardId = GetAndRefreshExpectedShardId(entity);
                Preconditions.CheckNotNull(expectedShardId);

                var entityName = this.shardedSession.GuessEntityName(entity);
                foreach (var associatedShardId in this.shardedSession.GetAssociatedShardIds(entityName, entity))
                {
                    ThrowIfConflicitingShardId(entityName, expectedShardId, associatedShardId);
                }

                return false;
            }

            private ShardId GetAndRefreshExpectedShardId(object obj)
            {
                ShardId expectedShardId = shardedSession.GetShardIdForObject(obj);
                if (expectedShardId == null)
                {
                    expectedShardId = CurrentSubgraphShardId;
                }
                else
                {
                    CurrentSubgraphShardId = expectedShardId;
                }
                return expectedShardId;
            }
        }

        private class CrossShardRelationshipDetectorDecorator : InterceptorDecorator
        {
            private readonly CrossShardRelationshipDetector detector;

            public CrossShardRelationshipDetectorDecorator(
                    CrossShardRelationshipDetector detector, IInterceptor delegateInterceptor)
                : base(delegateInterceptor)
            {
                this.detector = detector;
            }

            public override bool OnFlushDirty(object entity, object id, object[] currentState, object[] previousState, string[] propertyNames, NHibernate.Type.IType[] types)
            {
                this.detector.OnFlushDirty(entity, id, currentState, previousState, propertyNames, types);
                return this.delegateInterceptor.OnFlushDirty(entity, id, currentState, previousState, propertyNames, types);
            }

            public override void OnCollectionUpdate(object collection, object key)
            {
                this.detector.OnCollectionUpdate(collection, key);
                this.delegateInterceptor.OnCollectionUpdate(collection, key);
            }
        }

        #endregion
    }
}