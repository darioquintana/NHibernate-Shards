using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Iesi.Collections.Generic;
using log4net;
using System.Linq;
using NHibernate.Engine;
using NHibernate.Id;
using NHibernate.Metadata;
using NHibernate.Proxy;
using NHibernate.Shards.Criteria;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Id;
using NHibernate.Shards.Query;
using NHibernate.Shards.Stat;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Strategy.Selection;
using NHibernate.Shards.Transaction;
using NHibernate.Shards.Util;
using NHibernate.Stat;
using NHibernate.Type;

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
        [ThreadStatic]
        private static ShardId currentSubgraphShardId;

        private readonly bool checkAllAssociatedObjectsForDifferentShards;
        private readonly Set<System.Type> classesWithoutTopLevelSaveSupport;
        private readonly ILog log = LogManager.GetLogger(typeof(ShardedSessionImpl));

        private readonly IShardedSessionFactoryImplementor shardedSessionFactory;

        private readonly IDictionary<ShardId, IShard> shardIdsToShards;
        private readonly List<IShard> shards;

        private readonly IShardStrategy shardStrategy;

        private bool closed = false;

        private bool lockedShard = false;

        private ShardId lockedShardId;

        // access to sharded session is single-threaded so we can use a non-atomic
        // counter for criteria ids or query ids
        private int nextCriteriaId = 0;
        private int nextQueryId = 0;
        private IShardedTransaction transaction;

		/**
		 * Constructor used for openSession(...) processing.
		 *
		 * @param shardedSessionFactory The factory from which this session was obtained
		 * @param shardStrategy The shard strategy for this session
		 * @param classesWithoutTopLevelSaveSupport The set of classes on which top-level save can not be performed
		 * @param checkAllAssociatedObjectsForDifferentShards Should we check for cross-shard relationships
		 */
        internal ShardedSessionImpl(
            IShardedSessionFactoryImplementor shardedSessionFactory,
            IShardStrategy shardStrategy,
            Set<System.Type> classesWithoutTopLevelSaveSupport,
            bool checkAllAssociatedObjectsForDifferentShards)
            : this(null,
                   shardedSessionFactory,
                   shardStrategy,
                   classesWithoutTopLevelSaveSupport,
                   checkAllAssociatedObjectsForDifferentShards)
        {
        }

		/**
		 * Constructor used for openSession(...) processing.
		 *
		 * @param interceptor The interceptor to be applied to this session
		 * @param shardedSessionFactory The factory from which this session was obtained
		 * @param shardStrategy The shard strategy for this session
		 * @param classesWithoutTopLevelSaveSupport The set of classes on which top-level save can not be performed
		 * @param checkAllAssociatedObjectsForDifferentShards Should we check for cross-shard relationships
		 */
        internal ShardedSessionImpl(
            /*@Nullable*/ IInterceptor interceptor,
                          IShardedSessionFactoryImplementor shardedSessionFactory,
                          IShardStrategy shardStrategy,
                          Set<System.Type> classesWithoutTopLevelSaveSupport,
                          bool checkAllAssociatedObjectsForDifferentShards)
        {
            this.shardedSessionFactory = shardedSessionFactory;
            shards =
                BuildShardListFromSessionFactoryShardIdMap(shardedSessionFactory.GetSessionFactoryShardIdMap(),
                                                           checkAllAssociatedObjectsForDifferentShards, this, interceptor);

            shardIdsToShards = BuildShardIdsToShardsMap();
            this.shardStrategy = shardStrategy;
            this.classesWithoutTopLevelSaveSupport = classesWithoutTopLevelSaveSupport;
            this.checkAllAssociatedObjectsForDifferentShards = checkAllAssociatedObjectsForDifferentShards;
        }

        public static ShardId CurrentSubgraphShardId
        {
            get { return currentSubgraphShardId; }
            set { currentSubgraphShardId = value; }
        }

        public ISession SomeSession
        {
            get
            {
                foreach (IShard shard in shards)
                {
                    if (shard.Session != null)
                    {
                        return shard.Session;
                    }
                }
                return null;
            }
        }

        #region IShardedSession Members

        /// <summary>
        /// Gets the non-sharded session with which the objects is associated.
        /// </summary>
        /// <param name="obj">the object for which we want the Session</param>
        /// <returns>
        ///	The Session with which this object is associated, or null if the
        /// object is not associated with a session belonging to this ShardedSession
        /// </returns>
        public ISession GetSessionForObject(object obj)
        {
            return GetSessionForObject(obj, shards);
        }

        /// <summary>
        ///  Gets the ShardId of the shard with which the objects is associated.
        /// </summary>
        /// <param name="obj">the object for which we want the Session</param>
        /// <returns>
        /// the ShardId of the Shard with which this object is associated, or
        /// null if the object is not associated with a shard belonging to this
        /// ShardedSession
        /// </returns>
        public ShardId GetShardIdForObject(object obj)
        {
            return GetShardIdForObject(obj, shards);
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
        /// Force the <c>ISession</c> to flush.
        /// </summary>
        /// <remarks>
        /// Must be called at the end of a unit of work, before commiting the transaction and closing
        /// the session (<c>Transaction.Commit()</c> calls this method). <i>Flushing</i> if the process
        /// of synchronising the underlying persistent store with persistable state held in memory.
        /// </remarks>
        public void Flush()
        {
            foreach (IShard shard in shards)
            {
                // unopened sessions won't have anything to flush
                if (shard.Session != null)
                {
                    shard.Session.Flush();
                }
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
            get
            {
                // all shards must have the same flush mode
                ISession someSession = SomeSession;
                if (someSession == null)
                {
                    someSession = shards[0].EstablishSession();
                }
                return someSession.FlushMode;
            }
            set
            {
                var @event = new SetFlushModeOpenSessionEvent(value);
                foreach (IShard shard in shards)
                {
                    if (shard.Session != null)
                    {
                        shard.Session.FlushMode = value;
                    }
                    else
                    {
                        shard.AddOpenSessionEvent(@event);
                    }
                }
            }
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
            get
            {
                // all shards must have the same cache mode
                ISession someSession = SomeSession;
                if (someSession == null)
                {
                    someSession = shards[0].EstablishSession();
                }
                return someSession.CacheMode;
            }
            set
            {
                var @event = new SetCacheModeOpenSessionEvent(value);
                foreach (IShard shard in shards)
                {
                    if (shard.Session != null)
                    {
                        shard.Session.CacheMode = value;
                    }
                    else
                    {
                        shard.AddOpenSessionEvent(@event);
                    }
                }
            }
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
            foreach (IShard shard in shards)
            {
                if (shard.Session != null)
                {
                	shard.Session.Disconnect();
                }
            }
			// we do not allow application-supplied connections, so we can always return
			// null
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
            throw new NotSupportedException();
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
            List<Exception> thrown = null;

            foreach (IShard shard in shards)
            {
                if (shard.Session != null)
                {
                    try
                    {
                        shard.Session.Close();
                    }
                    catch (Exception ex)
                    {
                        thrown = thrown ?? new List<Exception>();

                        thrown.Add(ex);
                        // we're going to try and close everything that was
                        // opened
                    }
                }
            }
            shards.Clear();

            shardIdsToShards.Clear();

            classesWithoutTopLevelSaveSupport.Clear();

            if (thrown != null && !(thrown.Count == 0))
            {
                // we'll just throw the first one
                Exception first = thrown[0];
                if (typeof(HibernateException).IsAssignableFrom(first.GetType()))
                {
                    throw (HibernateException)first;
                }
                throw new HibernateException(first);
            }

            closed = true;

            // TODO what should I return here?
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
            foreach (IShard shard in shards)
            {
                if (shard.Session != null)
                {
                    shard.Session.CancelQuery();
                }
            }
        }

        /// <summary>
        /// Is the <c>ISession</c> still open?
        /// </summary>
        public bool IsOpen
        {
            get
            {
				// one open session means the sharded session is open
                foreach(IShard shard in shards)
                {
                    if(shard.Session != null && shard.Session.IsOpen)
                    {
                        return true;
                    }
                }
                return !closed;
            }
        }

        /// <summary>
        /// Is the <c>ISession</c> currently connected?
        /// </summary>
        public bool IsConnected
        {
            get
            {
                // one connected shard means the session as a whole is connected
                foreach (IShard shard in shards)
                {
                    if (shard.Session != null)
                    {
                        if (shard.Session.IsConnected)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Does this <c>ISession</c> contain any changes which must be
        /// synchronized with the database? Would any SQL be executed if
        /// we flushed this session?
        /// </summary>
        public bool IsDirty()
        {
            // one dirty shard is all it takes
            foreach (IShard shard in shards)
            {
                if (shard.Session != null)
                    if (shard.Session.IsDirty())
                        return true;
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
            foreach (IShard shard in shards)
            {
                if (shard.Session != null)
                {
                    try
                    {
                        return shard.Session.GetIdentifier(obj);
                    }
                    catch (TransientObjectException e)
                    {
                        // Object is transient or is not associated with this session.
                    }
                }
            }
            throw new TransientObjectException("Instance is transient or associated with a defferent Session");
        }

        /// <summary>
        /// Is this instance associated with this Session?
        /// </summary>
        /// <param name="obj">an instance of a persistent class</param>
        /// <returns>true if the given instance is associated with this Session</returns>
        public bool Contains(object obj)
        {
            foreach (IShard shard in shards)
            {
                if (shard.Session != null)
                    if (shard.Session.Contains(obj))
                        return true;
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
            foreach (IShard shard in shards)
            {
                if (shard.Session != null)
                {
                    shard.Session.Evict(obj);
                }
            }
        }

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
            IList<ShardId> shardIds = SelectShardIdsFromShardResolutionStrategyData(
                new ShardResolutionStrategyDataImpl(clazz, id));
            if (shardIds.Count == 1)
            {
                return shardIdsToShards[shardIds[0]].EstablishSession().Load(clazz, id, lockMode);
            }
            else
            {
                Object result = Get(clazz, id, lockMode);
                if (result == null)
                {
                    shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(clazz.Name, id);
                }
                return result;
            }
        }

        public object Load(string entityName, object id, LockMode lockMode)
        {
            IList<ShardId> shardIds =
                SelectShardIdsFromShardResolutionStrategyData(new ShardResolutionStrategyDataImpl(entityName, id));
            if(shardIds.Count == 1)
            {
                return shardIdsToShards[shardIds[0]].EstablishSession().Load(entityName, id, lockMode);
            }
            object result = Get(entityName, id, lockMode);
            if(result == null)
            {
                shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(entityName, id);
            }
            return result;
        }

        private object Get(string entityName, object id, LockMode mode)
        {
            IShardOperation<object> shardOp = new GetShardOperationByEntityNameIdAndLockMode(entityName, (ISerializable) id, mode);
			// we're not letting people customize shard selection by lockMode
            return ApplyGetOperation(shardOp, new ShardResolutionStrategyDataImpl(entityName, id));
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
            IList<ShardId> shardIds = SelectShardIdsFromShardResolutionStrategyData(new
                                                                                    ShardResolutionStrategyDataImpl(clazz, id));
            if (shardIds.Count == 1)
            {
                return shardIdsToShards[shardIds[0]].EstablishSession().Load(clazz, id);
            }
            Object result = this.Get(clazz, id);
            if (result == null)
            {
                this.shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(clazz.Name, id);
            }
            return result;
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
            return (T) Load(typeof (T), id, lockMode);
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
            return (T) Load(typeof (T), id);
        }

        public object Load(string entityName, object id)
        {
            IList<ShardId> shardIds = SelectShardIdsFromShardResolutionStrategyData(new
                                                                                    ShardResolutionStrategyDataImpl(entityName, id));
            if (shardIds.Count == 1)
            {
                return shardIdsToShards[shardIds[0]].EstablishSession().Load(entityName, id);
            }
            else
            {
                Object result = Get(entityName, id);
                if (result == null)
                {
                    shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(entityName, id);
                }
                return result;
            }
        }

        /// <summary>
        /// Read the persistent state associated with the given identifier into the given transient 
        /// instance.
        /// </summary>
        /// <param name="obj">An "empty" instance of the persistent class</param>
        /// <param name="id">A valid identifier of an existing persistent instance of the class</param>
        public void Load(object obj, object id)
        {
            IList<ShardId> shardIds =
                SelectShardIdsFromShardResolutionStrategyData(new ShardResolutionStrategyDataImpl(obj.GetType(), id));
            if (shardIds.Count == 1)
            {
                shardIdsToShards[shardIds[0]].EstablishSession().Load(obj, id);
            }
            else
            {
                object result = Get(obj.GetType(), id);
                if (result == null)
                {
                    shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(obj.GetType().Name, id);
                }
                else
                {
                    IShard objectShard = GetShardForObject(result, ShardIdListToShardList(shardIds));
                    Evict(result);
                    objectShard.EstablishSession().Load(obj, id);
                }

            }
        }

        /// <summary>
        /// Persist all reachable transient objects, reusing the current identifier 
        /// values. Note that this will not trigger the Interceptor of the Session.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="replicationMode"></param>
        public void Replicate(object obj, ReplicationMode replicationMode)
        {
            Replicate(null, obj, replicationMode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="obj"></param>
        /// <param name="replicationMode"></param>
        public void Replicate(string entityName, object obj, ReplicationMode replicationMode)
        {
            ISerializable id = ExtractId(obj);
            IList<ShardId> shardIds =
                SelectShardIdsFromShardResolutionStrategyData(new ShardResolutionStrategyDataImpl(obj.GetType(), id));
            if(shardIds.Count == 1)
            {
                CurrentSubgraphShardId = shardIds[0];
                shardIdsToShards[shardIds[0]].EstablishSession().Replicate(entityName, obj, replicationMode);
            }
            else
            {
                object result = null;
                if(id != null)
                {
                    result = Get(obj.GetType(), id);                    
                }
                if(result == null)
                {
                    ShardId shardId = SelectShardIdForNewObject(obj);
                    CurrentSubgraphShardId = shardId;
                    shardIdsToShards[shardId].EstablishSession().Replicate(entityName, obj, replicationMode);
                }
                else
                {
                    IShard objectShard = GetShardForObject(result, ShardIdListToShardList(shardIds));
                    Evict(result);
                    objectShard.EstablishSession().Replicate(entityName, obj, replicationMode);
                }
            }

        }

        ISerializable ExtractId(object obj)
        {
            IClassMetadata cmd = shardedSessionFactory.GetClassMetadata(obj.GetType());
			// I'm just guessing about the EntityMode
            return (ISerializable) cmd.GetIdentifier(obj, EntityMode.Poco);
        }

        /// <summary>
        /// Persist the given transient instance, first assigning a generated identifier.
        /// </summary>
        /// <remarks>
        /// Save will use the current value of the identifier property if the <c>Assigned</c>
        /// generator is used.
        /// </remarks>
        /// <param name="obj">A transient instance of a persistent class</param>
        /// <returns>The generated identifier</returns>
        public object Save(object obj)
        {
            return Save(null, obj);
        }

        public object Save(string entityName, object obj)
        {
            // TODO: what if we have detached instance?
            ShardId shardId = GetShardIdForObject(obj);
            if (shardId == null)
            {
                shardId = SelectShardIdForNewObject(obj);
            }
            Preconditions.CheckNotNull(shardId);
            SetCurrentSubgraphShardId(shardId);
            log.Debug(String.Format("Saving object of type {0} to shard {1}", obj.GetType(), shardId));
            return shardIdsToShards[shardId].EstablishSession().Save(entityName, obj);

        }

        public static void SetCurrentSubgraphShardId(ShardId shardId)
        {
        	currentSubgraphShardId = shardId;           
        }

		/*
		 * We already know that we don't have a shardId locked in for this session,
		 * and we already know that this object can't grab its session from some
		 * other object (we looked).  If this class is in the set of classes
		 * that don't support top-level saves, it's an error.
		 * This is to prevent clients from accidentally splitting their object graphs
		 * across multiple shards.
		 */
        private void CheckForUnsupportedToplevelSave(System.Type clazz)
        {
            if(classesWithoutTopLevelSaveSupport.Contains(clazz))
            {
                string msg = string.Format("Attempt to save object of type {0} as top-level object", clazz.Name);
                log.Error(msg);
                throw new HibernateException(msg);
            }
        }

        ShardId SelectShardIdForNewObject(object obj)
        {
            if (lockedShardId != null)
            {
                return lockedShardId;
            }
            ShardId shardId;
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
            shardId = GetShardIdOfRelatedObject(obj);
            if(shardId == null)
            {
                CheckForUnsupportedToplevelSave(obj.GetType());
                shardId = shardStrategy.ShardSelectionStrategy.SelectShardIdForNewObject(obj);             
            }
			// lock has been requested but shard has not yet been selected - lock it in
            if(lockedShard)
            {
                lockedShardId = shardId;
            }
            
            log.Debug(string.Format("Selected shard {0} for object of type {1}", shardId.Id, obj.GetType().Name));
            
            return shardId;
            
        }

		/**
		 * TODO(maxr) I can see this method benefitting from a cache that lets us quickly
		 * see which properties we might need to look at.
		 */
        ShardId GetShardIdOfRelatedObject(object obj)
        {
            IClassMetadata cmd = GetClassMetadata(obj.GetType());
            var types = cmd.GetPropertyValues(obj, EntityMode.Poco); // this wasn't in java null, EntityMode.Poco
            // TODO() fix hard-coded entity mode
            object[] values = cmd.GetPropertyValues(obj, EntityMode.Poco);
            ShardId shardId = null;
            List<Collection<Object>> collections = null;
            foreach (KeyValuePair<IType, object> pair in CrossShardRelationshipDetectingInterceptor.BuildListOfAssociations(new IType[] {}, new object[] {}))//types, values
            {
                if (pair.Key.IsCollectionType) //pair.getFirst().isCollectionType()
                {
                    /**
                     * collection types are more expensive to evaluate (might involve
                     * lazy-loading the contents of the collection from the db), so
                     * let's hold off until the end on the chance that we can fail
                     * quickly.
                     */
                    if (collections == null)
                    {
                        collections = new List<Collection<object>>();
                    }
                    
                    var coll = (Collection<Object>)pair.Value; 
                    collections.Add(coll);
                }
                else
                {
                    shardId = CheckForConflictingShardId(shardId, obj.GetType(), pair.Value);
                    /**
                     * if we're not checking for different shards, return as soon as we've
                     * got one
                     */
                    if (shardId != null && !checkAllAssociatedObjectsForDifferentShards)
                    {
                        return shardId;
                    }
                }
            }
            if (collections != null)
            {
                foreach (Object collEntry in Iterables.Concatenation(collections))
                {
                    shardId = CheckForConflictingShardId(shardId, obj.GetType(), collEntry);
                    if (shardId != null && !checkAllAssociatedObjectsForDifferentShards)
                    {
                        /**
                         * if we're not checking for different shards, return as soon as we've
                         * got one
                         */
                        return shardId;
                    }
                }
            }
            return shardId;
        }

        ShardId CheckForConflictingShardId(ShardId existingShardId, System.Type newObjectClass, Object associatedObject)
        {
            ShardId localShardId = GetShardIdForObject(associatedObject);
            if (localShardId != null)
            {
                if (existingShardId == null)
                {
                    existingShardId = localShardId;
                }
                else if (!localShardId.Equals(existingShardId))
                {
                    /*readonly*/
                    string msg = string.Format(
                        "Object of type {0} is on shard {1} but an associated object of type {2} is on shard {3}.",
                        newObjectClass.Name,
                        existingShardId.Id,
                        associatedObject.GetType().Name,
                        localShardId.Id);
                    log.Error(msg);
                    throw new CrossShardAssociationException(msg);
                }
            }
            return existingShardId;
        }


        IClassMetadata GetClassMetadata(System.Type clazz)
        {
            return GetShardedSessionFactory().GetClassMetadata(clazz);
        }

        private IShardedSessionFactoryImplementor GetShardedSessionFactory()
        {
            return shardedSessionFactory; // why when I set as property expects a Delegate? 
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

        /// <summary>
        /// Either <c>Save()</c> or <c>Update()</c> the given instance, depending upon the value of
        /// its identifier property.
        /// </summary>
        /// <remarks>
        /// By default the instance is always saved. This behaviour may be adjusted by specifying
        /// an <c>unsaved-value</c> attribute of the identifier property mapping
        /// </remarks>
        /// <param name="obj">A transient instance containing new or updated state</param>
        public void SaveOrUpdate(object obj)
        {
            ApplySaveOrUpdateOperation(new SaveOrUpdateSimple(), obj);
        }

        void ApplySaveOrUpdateOperation(ISaveOrUpdateOperation op, object obj)
        {
            ShardId shardId = GetShardIdForObject(obj);
            if(shardId != null)
            {
				// attached object
                op.SaveOrUpdate(shardIdsToShards[shardId], obj);
                return;
            }
            IList<IShard> potentialShards = DetermineShardsObjectViaResolutionStrategy(obj);
            if(potentialShards.Count == 1)
            {
                op.SaveOrUpdate(potentialShards[0], obj);
                return;
            }

			/**
			 * Too bad, we've got a detached object that could be on more than 1 shard.
			 * The only safe way to handle this is to try and lookup the object, and if
			 * it exists, do a merge, and if it doesn't, do a save.
			 */
            ISerializable id = ExtractId(obj);
            if(id != null)
            {
                object persistent = Get(obj.GetType(), id);
                if(persistent != null)
                {
                    shardId = GetShardIdForObject(persistent);
                }
            }
            if(shardId != null)
            {
                op.Merge(shardIdsToShards[shardId], obj);
            }
            else
            {
                Save(obj);
            }
        }

        IList<IShard> DetermineShardsObjectViaResolutionStrategy(object obj)
        {
            ISerializable id = ExtractId(obj);
            if(id == null)
            {
                return new List<IShard>();
            }
            var srsd = new ShardResolutionStrategyDataImpl(obj.GetType(), id);
            IList<ShardId> shardIds = SelectShardIdsFromShardResolutionStrategyData(srsd);
            return ShardIdListToShardList(shardIds);
        }

        public void SaveOrUpdate(string entityName, object obj)
        {
            ISaveOrUpdateOperation so = new SaveOrUpdateWithEntityName(entityName);
            ApplySaveOrUpdateOperation(so, obj);
        }

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

            IUpdateOperation updateOperation = new UpdateOperationSimple();
            ApplyUpdateOperation(updateOperation, obj);
        }


        private void ApplyUpdateOperation(IUpdateOperation updateOperation, object obj)
        {
            ShardId shardId = GetShardIdForObject(obj);
            if(shardId != null)
            {
				// attached object
                updateOperation.Update(shardIdsToShards[shardId], obj);
                return;
            }
            IList<IShard> potentialShards = DetermineShardsObjectViaResolutionStrategy(obj);
            if(potentialShards.Count  == 1)
            {
                updateOperation.Update(potentialShards[0], obj);
                return;
            }
			/**
			  * Too bad, we've got a detached object that could be on more than 1 shard.
			  * The only safe way to perform the update is to load the object and then
			  * do a merge.
			  */
            ISerializable id = ExtractId(obj);
            if(id != null)
            {
                object persistent = Get(obj.GetType(), id);
                if(persistent != null)
                {
                    shardId = GetShardIdForObject(persistent);
                }
                
            }
            if(shardId == null)
            {
                /**
                 * This is an error condition.  In order to provide the same behavior
                 * as a non-sharded session we're just going to dispatch the update
                 * to a random shard (we know it will fail because either we don't have
                 * an id or the lookup returned).
                 */
                updateOperation.Update(Shards[0], obj);
                // this call may succeed but the commit will fail
            }
            else
            {
                updateOperation.Merge(shardIdsToShards[shardId], obj);
            }
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

        public void Update(string entityName, object obj)
        {
            IUpdateOperation updateOperation = new UpdateOperationWithEntityName(entityName);
            ApplyUpdateOperation(updateOperation, obj);
        }

        public object Merge(object obj)
        {
            return Merge(null, obj);
        }

        public object Merge(string entityName, object obj)
        {
            ISerializable id = ExtractId(obj);
            IList<ShardId> shardIds =
                SelectShardIdsFromShardResolutionStrategyData(new ShardResolutionStrategyDataImpl(obj.GetType(), id));
            if(shardIds.Count  == 1)
            {
                SetCurrentSubgraphShardId(shardIds[0]);
                return shardIdsToShards[shardIds[0]].EstablishSession().Merge(entityName, obj);
            }
            object result = null;
            if(id != null)
            {
                result = Get(obj.GetType(), id);
            }
            if(result == null)
            {
                ShardId shardId = SelectShardIdForNewObject(obj);
                SetCurrentSubgraphShardId(shardId);
                return shardIdsToShards[shardId].EstablishSession().Merge(entityName, obj);
            }
            IShard objectShard = GetShardForObject(result, ShardIdListToShardList(shardIds));
            return objectShard.EstablishSession().Merge(entityName, obj);
        }

        public void Persist(object obj)
        {
            Persist(null, obj);
        }

        public void Persist(string entityName, object obj)
        {
            //TODO: what if we have detached object?
            ShardId shardId = GetShardIdForObject(obj);
            if(shardId == null)
            {
                shardId = SelectShardIdForNewObject(obj);
            }
            Preconditions.CheckNotNull(shardId);
            SetCurrentSubgraphShardId(shardId);
            log.Debug(string.Format("Persisting object of type {0} to shard {1}", obj.GetType(), shardId));
            shardIdsToShards[shardId].EstablishSession().Persist(entityName, obj);
        }

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
            throw new NotSupportedException();
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
            throw new NotSupportedException();
        }

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
            IDeleteOperation deleteOperation = new DeleteOperationSimple();
            ApplyDeleteOperation(deleteOperation, obj);
        }

        public void Delete(string entityName, object obj)
        {
            IDeleteOperation deleteOperation = new DeleteOperationWithEntityName(entityName);
            ApplyDeleteOperation(deleteOperation, obj);
        }

        private void ApplyDeleteOperation(IDeleteOperation deleteOperation, object obj)
        {
            ShardId shardId = GetShardIdForObject(obj);
            if (shardId != null)
            {
				// attached object
                deleteOperation.Delete(shardIdsToShards[shardId], obj);
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
        	IList<IShard> potentialShards = DetermineShardsObjectViaResolutionStrategy(obj);
			if(potentialShards.Count == 1)
			{
				deleteOperation.Delete(potentialShards[0], obj);
				return;
			}
			/**
			 * Too bad, we've got a detached object that could be on more than 1 shard.
			 * The only safe way to perform the delete is to load the object before
			 * deleting.
			 */
        	object persistent = Get(obj.GetType(), ExtractId(obj));
        	shardId = GetShardIdForObject(persistent);
        	deleteOperation.Delete(shardIdsToShards[shardId], persistent);

        }

        /// <summary>
        /// Execute a query
        /// </summary>
        /// <param name="query">A query expressed in Hibernate's query language</param>
        /// <returns>A distinct list of instances</returns>
        /// <remarks>See <see cref="IQuery.List()"/> for implications of <c>cache</c> usage.</remarks>
        public IList Find(string query)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Execute a query, binding a value to a "?" parameter in the query string.
        /// </summary>
        /// <param name="query">The query string</param>
        /// <param name="value">A value to be bound to a "?" placeholder</param>
        /// <param name="type">The Hibernate type of the value</param>
        /// <returns>A distinct list of instances</returns>
        /// <remarks>See <see cref="IQuery.List()"/> for implications of <c>cache</c> usage.</remarks>
        public IList Find(string query, object value, IType type)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Execute a query, binding an array of values to a "?" parameters in the query string.
        /// </summary>
        /// <param name="query">The query string</param>
        /// <param name="values">An array of values to be bound to the "?" placeholders</param>
        /// <param name="types">An array of Hibernate types of the values</param>
        /// <returns>A distinct list of instances</returns>
        /// <remarks>See <see cref="IQuery.List()"/> for implications of <c>cache</c> usage.</remarks>
        public IList Find(string query, object[] values, IType[] types)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Execute a query and return the results in an interator.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the query has multiple return values, values will be returned in an array of 
        /// type <c>object[]</c>.
        /// </para>
        /// <para>
        /// Entities returned as results are initialized on demand. The first SQL query returns
        /// identifiers only. So <c>Enumerator()</c> is usually a less efficient way to retrieve
        /// object than <c>List()</c>.
        /// </para>
        /// </remarks>
        /// <param name="query">The query string</param>
        /// <returns>An enumerator</returns>
        public IEnumerable Enumerable(string query)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Execute a query and return the results in an interator, 
        /// binding a value to a "?" parameter in the query string.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the query has multiple return values, values will be returned in an array of 
        /// type <c>object[]</c>.
        /// </para>
        /// <para>
        /// Entities returned as results are initialized on demand. The first SQL query returns
        /// identifiers only. So <c>Enumerator()</c> is usually a less efficient way to retrieve
        /// object than <c>List()</c>.
        /// </para>
        /// </remarks>
        /// <param name="query">The query string</param>
        /// <param name="value">A value to be written to a "?" placeholder in the query string</param>
        /// <param name="type">The hibernate type of the value</param>
        /// <returns>An enumerator</returns>
        public IEnumerable Enumerable(string query, object value, IType type)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Execute a query and return the results in an interator, 
        /// binding the values to "?"s parameters in the query string.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the query has multiple return values, values will be returned in an array of 
        /// type <c>object[]</c>.
        /// </para>
        /// <para>
        /// Entities returned as results are initialized on demand. The first SQL query returns
        /// identifiers only. So <c>Enumerator()</c> is usually a less efficient way to retrieve
        /// object than <c>List()</c>.
        /// </para>
        /// </remarks>
        /// <param name="query">The query string</param>
        /// <param name="values">A list of values to be written to "?" placeholders in the query</param>
        /// <param name="types">A list of hibernate types of the values</param>
        /// <returns>An enumerator</returns>
        public IEnumerable Enumerable(string query, object[] values, IType[] types)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Apply a filter to a persistent collection.
        /// </summary>
        /// <remarks>
        /// A filter is a Hibernate query that may refer to <c>this</c>, the collection element.
        /// Filters allow efficient access to very large lazy collections. (Executing the filter
        /// does not initialize the collection.)
        /// </remarks>
        /// <param name="collection">A persistent collection to filter</param>
        /// <param name="filter">A filter query string</param>
        /// <returns>The resulting collection</returns>
        public ICollection Filter(object collection, string filter)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Apply a filter to a persistent collection, binding the given parameter to a "?" placeholder
        /// </summary>
        /// <remarks>
        /// A filter is a Hibernate query that may refer to <c>this</c>, the collection element.
        /// Filters allow efficient access to very large lazy collections. (Executing the filter
        /// does not initialize the collection.)
        /// </remarks>
        /// <param name="collection">A persistent collection to filter</param>
        /// <param name="filter">A filter query string</param>
        /// <param name="value">A value to be written to a "?" placeholder in the query</param>
        /// <param name="type">The hibernate type of value</param>
        /// <returns>A collection</returns>
        public ICollection Filter(object collection, string filter, object value, IType type)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Apply a filter to a persistent collection, binding the given parameters to "?" placeholders.
        /// </summary>
        /// <remarks>
        /// A filter is a Hibernate query that may refer to <c>this</c>, the collection element.
        /// Filters allow efficient access to very large lazy collections. (Executing the filter
        /// does not initialize the collection.)
        /// </remarks>
        /// <param name="collection">A persistent collection to filter</param>
        /// <param name="filter">A filter query string</param>
        /// <param name="values">The values to be written to "?" placeholders in the query</param>
        /// <param name="types">The hibernate types of the values</param>
        /// <returns>A collection</returns>
        public ICollection Filter(object collection, string filter, object[] values, IType[] types)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Delete all objects returned by the query.
        /// </summary>
        /// <param name="query">The query string</param>
        /// <returns>Returns the number of objects deleted.</returns>
        public int Delete(string query)
        {
            throw new NotSupportedException();
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
            throw new NotSupportedException();
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
            throw new NotSupportedException();
        }

        /// <summary>
        /// Obtain the specified lock level upon the given object.
        /// </summary>
        /// <param name="obj">A persistent instance</param>
        /// <param name="lockMode">The lock level</param>
        public void Lock(object obj, LockMode lockMode)
        {
            IShardOperation<object> shardOp = new LockShardOperationByObjectAndLockMode(obj, lockMode);
            InvokeOnShardWithObject(shardOp, obj);
        }

        public void Lock(string entityName, object obj, LockMode lockMode)
        {
            IShardOperation<object> shardOp = new LockShardOperationByEntityNameObjectLockMode(entityName, obj, lockMode);
            InvokeOnShardWithObject(shardOp, obj);
        }

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
            IRefreshOperation refreshOperation = new RefreshOperationSimple();
            ApplyRefreshOperation(refreshOperation, obj);
        }

        private void ApplyRefreshOperation(IRefreshOperation refreshOperation, object obj)
        {
            ShardId shardId = GetShardIdForObject(obj);
            if(shardId != null)
            {
                refreshOperation.Refresh(shardIdsToShards[shardId], obj);
            }
            else
            {
                IList<IShard> candidateShards = DetermineShardsObjectViaResolutionStrategy(obj);
                if (candidateShards.Count == 1)
                {
                    refreshOperation.Refresh(candidateShards[0], obj);
                }
                else
                {
                    foreach(IShard shard in candidateShards)
                    {
                        try
                        {
                            refreshOperation.Refresh(shard, obj);
                        }
                        catch(UnresolvableObjectException)
                        {
                            //ignore
                        }
                    }
                    refreshOperation.Refresh(shards[0], obj);
                }
            }
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
            IRefreshOperation refreshOperation = new RefreshOperationWithLockMode(lockMode);
            ApplyRefreshOperation(refreshOperation, obj);
        }

        /// <summary>
        /// Determine the current lock mode of the given object
        /// </summary>
        /// <param name="obj">A persistent instance</param>
        /// <returns>The current lock mode</returns>
        public LockMode GetCurrentLockMode(object obj)
        {
            IShardOperation<LockMode> shardOp = new CurrentLockModeShardOperation(obj);
            return InvokeOnShardWithObject(shardOp, obj);
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
            ErrorIfClosed();
            ITransaction result = GetTransaction(IsolationLevel.Unspecified);
            result.Begin();
            return result;
        }
        public ITransaction GetTransaction(IsolationLevel isoLevel)
        {
            ErrorIfClosed();
            if (transaction == null)
            {
                transaction = new ShardedTransactionImpl(this,isoLevel);
            }
            return transaction;
        }
        void ErrorIfClosed()
        {
            if (closed)
            {
                throw new SessionException("Session is closed!");
            }
        }

        /// <summary>
        /// Begin a transaction with the specified <c>isolationLevel</c>
        /// </summary>
        /// <param name="isolationLevel">Isolation level for the new transaction</param>
        /// <returns>A transaction instance having the specified isolation level</returns>
        public ITransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            ErrorIfClosed();
            ITransaction result = GetTransaction(isolationLevel);
            result.Begin();
            return result;

        }

        public ICriteria CreateCriteria<T>() where T : class
        {
            return CreateCriteria(typeof (T));
        }

        public ICriteria CreateCriteria<T>(string alias) where T : class
        {
            return CreateCriteria(typeof (T), alias);
        }

        /// <summary>
        /// Get the current Unit of Work and return the associated <c>ITransaction</c> object.
        /// </summary>
        public ITransaction Transaction
        {
            get 
            {
                ErrorIfClosed();
                if(transaction == null)
                {
                    transaction = new ShardedTransactionImpl(this);
                }
                return transaction;
            }
        }

        /// <summary>
        /// Creates a new <c>Criteria</c> for the entity class.
        /// </summary>
        /// <param name="persistentClass">The class to Query</param>
        /// <returns>An ICriteria object</returns>
        public ICriteria CreateCriteria(System.Type persistentClass)
        {
            return new ShardedCriteriaImpl(new CriteriaId(nextCriteriaId++), shards,
                                           new CriteriaFactoryImpl(persistentClass), shardStrategy.ShardAccessStrategy);
        }

        /// <summary>
        /// Creates a new <c>Criteria</c> for the entity class with a specific alias
        /// </summary>
        /// <param name="persistentClass">The class to Query</param>
        /// <param name="alias">The alias of the entity</param>
        /// <returns>An ICriteria object</returns>
        public ICriteria CreateCriteria(System.Type persistentClass, string alias)
        {
            return new ShardedCriteriaImpl(new CriteriaId(nextCriteriaId++), shards,
                                            new CriteriaFactoryImpl(persistentClass, alias), shardStrategy.ShardAccessStrategy);
        }

        public ICriteria CreateCriteria(string entityName)
        {
            return new ShardedCriteriaImpl(new CriteriaId(nextCriteriaId++), shards,
                                           new CriteriaFactoryImpl(entityName), shardStrategy.ShardAccessStrategy);
        }

        public ICriteria CreateCriteria(string entityName, string alias)
        {
            return new ShardedCriteriaImpl(new CriteriaId(nextCriteriaId++), shards,
                                           new CriteriaFactoryImpl(entityName,alias), shardStrategy.ShardAccessStrategy);
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
            return new ShardedQueryImpl(new QueryId(nextQueryId++), shards, new AdHocQueryFactoryImpl(queryString),
                                        shardStrategy.ShardAccessStrategy);
        }

        public IQuery CreateQuery(IQueryExpression queryExpression)
        {
            throw new NotSupportedException();
        }

		//public IQuery CreateQuery(IQueryExpression queryExpression)
		//{
		//    throw new NotSupportedException();
		//}

        /// <summary>
        /// Create a new instance of <c>Query</c> for the given collection and filter string
        /// </summary>
        /// <param name="collection">A persistent collection</param>
        /// <param name="queryString">A hibernate query</param>
        /// <returns>A query</returns>
        public IQuery CreateFilter(object collection, string queryString)
        {
            IShard shard = GetShardForCollection(collection,shards);
            ISession session;
            if(shard == null)
            {
				// collection not associated with any of our shards, so just delegate to
				// a random shard.  We'll end up failing, but we'll fail with the
				// error that users typically get.
                session = SomeSession;
                if(session!=null )
                {
                    session = shards[0].EstablishSession();
                }
            }
            else
            {
                session = shard.EstablishSession();
            }
            return session.CreateFilter(collection, queryString);
        }

        private IShard GetShardForCollection(object collection, IList<IShard> shardsToConsider)
        {
            foreach(IShard shard in shardsToConsider)
            {
                if(shard.Session != null)
                {
                    var si = (ISessionImplementor) shard.Session;
                    if(si.PersistenceContext.GetCollectionEntryOrNull(collection) != null)
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
            return new ShardedQueryImpl(new QueryId(nextQueryId++), shards, new NamedQueryFactoryImpl(queryName),
                                        shardStrategy.ShardAccessStrategy);
        }

        /// <summary>
        /// Create a new instance of <c>IQuery</c> for the given SQL string.
        /// </summary>
        /// <param name="sql">a query expressed in SQL</param>
        /// <param name="returnAlias">a table alias that appears inside <c>{}</c> in the SQL string</param>
        /// <param name="returnClass">the returned persistent class</param>
        /// <returns>An <see cref="IQuery"/> from the SQL string</returns>
        public IQuery CreateSQLQuery(string sql, string returnAlias, System.Type returnClass)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Create a new instance of <see cref="IQuery" /> for the given SQL string.
        /// </summary>
        /// <param name="sql">a query expressed in SQL</param>
        /// <param name="returnAliases">an array of table aliases that appear inside <c>{}</c> in the SQL string</param>
        /// <param name="returnClasses">the returned persistent classes</param>
        /// <returns>An <see cref="IQuery"/> from the SQL string</returns>
        public IQuery CreateSQLQuery(string sql, string[] returnAliases, System.Type[] returnClasses)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Create a new instance of <see cref="ISQLQuery" /> for the given SQL query string.
        /// </summary>
        /// <param name="queryString">a query expressed in SQL</param>
        /// <returns>An <see cref="ISQLQuery"/> from the SQL string</returns>
        public ISQLQuery CreateSQLQuery(string queryString)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Completely clear the session. Evict all loaded instances and cancel all pending
        /// saves, updates and deletions. Do not close open enumerables or instances of
        /// <c>ScrollableResults</c>.
        /// </summary>
        public void Clear()
        {
            foreach (IShard shard in shards)
            {
                if(shard.Session != null)
                {
                    shard.Session.Clear();
                }
            }
        }

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
            IShardOperation<Object> shardOp = new GetShardOperationByTypeAndId(clazz, id);
            return ApplyGetOperation(shardOp, new ShardResolutionStrategyDataImpl(clazz, id));
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
            IShardOperation<object> shardOp = new GetShardOperationByTypeIdAndLockMode(clazz, (ISerializable) id, lockMode);
			// we're not letting people customize shard selection by lockMode
            return ApplyGetOperation(shardOp, new ShardResolutionStrategyDataImpl(clazz, id));
        }

        public object Get(string entityName, object id)
        {
            IShardOperation<object> shardOp = new GetShardOperationByEntityNameAndId(entityName, (ISerializable) id);
			// we're not letting people customize shard selection by lockMode
            return ApplyGetOperation(shardOp, new ShardResolutionStrategyDataImpl(entityName, id));
        }

        /// <summary>
        /// Strongly-typed version of <see cref="ISession.Get(Type,object)" />
        /// </summary>
        public T Get<T>(object id)
        {
            return (T) this.Get(typeof (T), id);
        }

        /// <summary>
        /// Strongly-typed version of <see cref="ISession.Get(Type,object,LockMode)" />
        /// </summary>
        public T Get<T>(object id, LockMode lockMode)
        {
            return (T) this.Get(typeof (T), id, lockMode);
        }

        /// <summary> 
        /// Return the entity name for a persistent entity
        /// </summary>
        /// <param name="obj">a persistent entity</param>
        /// <returns> the entity name </returns>
        public string GetEntityName(object obj)
        {
            IShardOperation<string> invoker = new GetShardOperationByEntityName(obj);
            return InvokeOnShardWithObject(invoker, obj);
        }

		/**
		  * Helper method we can use when we need to find the Shard with which a
		  * specified object is associated and invoke the method on that Shard.
		  * If the object isn't associated with a Session we just invoke it on a
		  * random Session with the expectation that this will cause an error.
		  */
        T InvokeOnShardWithObject<T>(IShardOperation<T> so, object obj )
        {
            ShardId shardId = GetShardIdForObject(obj);
			// just ask this question of a random shard so we get the proper error
            IShard shardToUse = shardId == null ? this.shards[0] : this.shardIdsToShards[shardId];
            return so.Execute(shardToUse);
        }

        /// <summary>
        /// Enable the named filter for this current session.
        /// </summary>
        /// <param name="filterName">The name of the filter to be enabled.</param>
        /// <returns>The Filter instance representing the enabled fiter.</returns>
        public IFilter EnableFilter(string filterName)
        {
            var filterEvent = new EnableFilterOpenSessionEvent(filterName);
            foreach(IShard shard in shards)
            {
                if(shard.Session != null)
                {
                    shard.Session.EnableFilter(filterName);
                }
                else
                {
                    shard.AddOpenSessionEvent(filterEvent);
                }
            }
            //TODO: what do we do here?  A sharded filter?
            return null;
        }

        /// <summary>
        /// Retrieve a currently enabled filter by name.
        /// </summary>
        /// <param name="filterName">The name of the filter to be retrieved.</param>
        /// <returns>The Filter instance representing the enabled fiter.</returns>
        public IFilter GetEnabledFilter(string filterName)
        {
			// TODO(maxr) what do we return here?  A sharded filter?
            foreach(IShard shard in shards)
            {
                if(shard.Session != null)
                {
                    IFilter filter = shard.Session.GetEnabledFilter(filterName);
                    if(filter != null)
                    {
                        return filter;
                    }
                }
            }
            //TODO what do we do here?
            return null;
        }

        /// <summary>
        /// Disable the named filter for the current session.
        /// </summary>
        /// <param name="filterName">The name of the filter to be disabled.</param>
        public void DisableFilter(string filterName)
        {
            var filterEvent = new DisableFilterOpenSessionEvent(filterName);
            foreach(IShard shard in shards)
            {
                if (shard.Session != null)
                {
                    shard.Session.DisableFilter(filterName);
                }
                else
                {
                    shard.AddOpenSessionEvent(filterEvent);
                }
            }
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
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the batch size of the session
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public ISession SetBatchSize(int batchSize)
        {
            throw new NotSupportedException();
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

        /// <summary>
        /// An <see cref="IMultiCriteria"/> that can return a list of all the results
        /// of all the criterias.
        /// </summary>
        /// <returns></returns>
        public IMultiCriteria CreateMultiCriteria()
        {
            throw new NotSupportedException();
        }

        public ISession GetSession(EntityMode entityMode)
        {
            throw new NotSupportedException();
        }

        public EntityMode ActiveEntityMode
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary> Get the statistics for this session.</summary>
        public ISessionStatistics Statistics
        {
            get { return new ShardedSessionStatistics(this); }
        }

        ///<summary>
        ///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///</summary>
        ///<filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (!closed)
            {
                log.Warn("ShardedSessionImpl is being garbage collected but it was never properly closed.");
                try
                {
                    Close();
                }
                catch (Exception e)
                {
                    log.Warn("Caught exception trying to close.", e);
                }
            }
        }

        #endregion

        #region IShardedSessionImplementor Members

        /// <summary>
        /// Gets all the shards the ShardedSession is spanning.
        /// Return list of all shards the ShardedSession is associated with
        /// </summary>
        public IList<IShard> Shards
        {
            get { return shards.AsReadOnly(); } //Collections.unmodifiableList(shards);
        }

        #endregion

        private IList<ShardId> SelectShardIdsFromShardResolutionStrategyData(ShardResolutionStrategyDataImpl srsd)
        {
            IIdentifierGenerator idGenerator = shardedSessionFactory.GetIdentifierGenerator(srsd.EntityName);
            if ((idGenerator is IShardEncodingIdentifierGenerator) && (srsd.Id != null))
            {
				return new[] { ((IShardEncodingIdentifierGenerator)idGenerator).ExtractShardId(srsd.Id) };
            }
            return shardStrategy.ShardResolutionStrategy.SelectShardIdsFromShardResolutionStrategyData(srsd);
        }

        private object ApplyGetOperation(IShardOperation<object> shardOp, ShardResolutionStrategyDataImpl srsd)
        {
            IList<ShardId> shardIds = SelectShardIdsFromShardResolutionStrategyData(srsd);
            return shardStrategy.ShardAccessStrategy.Apply(ShardIdListToShardList(shardIds), shardOp,
                                                           new FirstNonNullResultExitStrategy<object>(),
                                                           new ExitOperationsQueryCollector());
        }

        private IList<IShard> ShardIdListToShardList(IEnumerable<ShardId> shardIds)
        {
            Set<IShard> shards = new HashedSet<IShard>();
            foreach(ShardId shardId in shardIds)
            {
                shards.Add(shardIdsToShards[shardId]);
            }
            return shards.ToList();
        }

        private ISession GetSessionForObject(object obj, List<IShard> shardsToConsider)
        {
            IShard shard = GetShardForObject(obj, shardsToConsider);
            if (shard == null)
            {
                return null;
            }
            return shard.Session;
        }

        private IShard GetShardForObject(object obj, IList<IShard> shardsToConsider)
        {            
            foreach (IShard shard in shardsToConsider)
            {
                if (shard.Session != null && shard.Session.Contains(obj))
                    return shard;
            }
            return null;
        }

        internal ShardId GetShardIdForObject(object obj, List<IShard> shardsToConsider)
        {
            //TODO: Also, wouldn't it be faster to first see if there's just a single shard id mapped to the shard?
			//TODO: optimize this by keeping an identity map of objects to shardId
            IShard shard = GetShardForObject(obj, shardsToConsider);

            if (shard == null)
            {
                return null;
            }
            if (shard.ShardIds.Count == 1)
            {
                IEnumerator<ShardId> iterator = shard.ShardIds.GetEnumerator();
                iterator.MoveNext();
                return iterator.Current;
            }
            string className;
            if (obj is INHibernateProxy)
                className = ((INHibernateProxy) obj).HibernateLazyInitializer.PersistentClass.Name;
            else
                className = obj.GetType().Name;


            IIdentifierGenerator idGenerator = shard.SessionFactoryImplementor.GetIdentifierGenerator(className);

            if (idGenerator is IShardEncodingIdentifierGenerator)
            {
                return ((IShardEncodingIdentifierGenerator)idGenerator).ExtractShardId(GetIdentifier(obj));
            }
            // TODO: also use shard resolution strategy if it returns only 1 shard; throw this error in config instead of here
            throw new HibernateException("Can not use virtual sharding with non-shard resolving id gen");
        }

        private IDictionary<ShardId, IShard> BuildShardIdsToShardsMap()
        {
            var map = new Dictionary<ShardId, IShard>();
            foreach (IShard shard in shards)
            {
                foreach (ShardId shardId in shard.ShardIds)
                {
                    map.Add(shardId, shard);
                }
            }
            return map;
        }

        private static List<IShard> BuildShardListFromSessionFactoryShardIdMap(
            IDictionary<ISessionFactoryImplementor, Set<ShardId>> sessionFactoryShardIdMap,
            bool checkAllAssociatedObjectsForDifferentShards,
            IShardIdResolver shardIdResolver,
            IInterceptor interceptor)
        {
            var shardList = new List<IShard>();
            foreach (var entry in sessionFactoryShardIdMap)
            {
                IOpenSessionEvent eventToRegister = null;
                IInterceptor interceptorToSet = interceptor;
                if(checkAllAssociatedObjectsForDifferentShards)
                {
					// cross shard association checks for updates are handled using interceptors
                    var csrdi =
                        new CrossShardRelationshipDetectingInterceptor(shardIdResolver);
                    if(interceptorToSet == null)
                    {
						// no interceptor to wrap so just use the cross-shard detecting interceptor raw
						// this is safe because it's a stateless interceptor
                        interceptorToSet = csrdi;
                    }
                    else
                    {
						// user specified their own interceptor, so wrap it with a decorator
						// that will still do the cross shard association checks
                        Pair<IInterceptor, IOpenSessionEvent> result = DecorateInterceptor(csrdi, interceptor);
                        interceptorToSet = result.first;
                        eventToRegister = result.second;
                    }
                }
                else if(interceptorToSet != null)
                {
					// user specified their own interceptor so need to account for the fact
					// that it might be stateful
                    Pair<IInterceptor, IOpenSessionEvent> result = HandleStatefulInterceptor(interceptorToSet);
                    interceptorToSet = result.first;
                    eventToRegister = result.second;
                }
                IShard shard = new ShardImpl(entry.Value,entry.Key,interceptorToSet);
                shardList.Add(shard);
                if(eventToRegister != null)
                {
                    shard.AddOpenSessionEvent(eventToRegister);
                }
            }
            return shardList;
        }

        internal static Pair<IInterceptor, IOpenSessionEvent> HandleStatefulInterceptor(IInterceptor mightBeStateful)
        {
            IOpenSessionEvent openSessionEvent = null;
            if (mightBeStateful.GetType().GetInterfaces().Contains(typeof(IStatefulInterceptorFactory)))
            {
                mightBeStateful = ((IStatefulInterceptorFactory)mightBeStateful).NewInstance();
                if (mightBeStateful.GetType().GetInterfaces().Contains(typeof(IRequiresSession)))
                {
                    openSessionEvent = new SetSessionOnRequiresSessionEvent((IRequiresSession)mightBeStateful);
                }                
            }
            return Pair<IInterceptor, IOpenSessionEvent>.Of(mightBeStateful, openSessionEvent);
        }

        static Pair<IInterceptor,IOpenSessionEvent> DecorateInterceptor(CrossShardRelationshipDetectingInterceptor csrdi,IInterceptor decorateMe)
        {
            Pair<IInterceptor, IOpenSessionEvent> pair = HandleStatefulInterceptor(decorateMe);
            IInterceptor decorator = new CrossShardRelationshipDetectingInterceptorDecorator(csrdi, pair.first);
            return Pair<IInterceptor, IOpenSessionEvent>.Of(decorator, pair.second);
        }

        public object Get(System.Type clazz, ISerializable id)
        {
            var shardOp = new GetShardOperationByTypeAndId(clazz, id);
            return ApplyGetOperation(shardOp, new ShardResolutionStrategyDataImpl(clazz, id));
        }

        public object Get(string entityName, ISerializable id)
        {
            var shardOp = new GetShardOperationByEntityNameAndId(entityName, id);
            return ApplyGetOperation(shardOp, new ShardResolutionStrategyDataImpl(entityName, id));
        }

        private class SaveOrUpdateWithEntityName:ISaveOrUpdateOperation
        {
            private string entityName;

            public SaveOrUpdateWithEntityName(string entityName)
            {
                this.entityName = entityName;
            }

            public void SaveOrUpdate(IShard shard, object obj)
            {
                shard.EstablishSession().SaveOrUpdate(entityName, obj);
            }

            public void Merge(IShard shard, object obj)
            {
                shard.EstablishSession().Merge(entityName, obj);
            }
        }

        private class SaveOrUpdateSimple:ISaveOrUpdateOperation
        {
            public void SaveOrUpdate(IShard shard, object obj)
            {
                shard.EstablishSession().SaveOrUpdate(obj);
            }

            public void Merge(IShard shard, object obj)
            {
                shard.EstablishSession().Merge(obj);
            }
        }

        private class UpdateOperationWithEntityName:IUpdateOperation
        {
            private string entityName;

            public UpdateOperationWithEntityName(string entityName)
            {
                this.entityName = entityName;
            }

            public void Update(IShard shard, object obj)
            {
                shard.EstablishSession().Update(entityName, obj);
            }

            public void Merge(IShard shard, object obj)
            {
                shard.EstablishSession().Merge(entityName, obj);
            }
        }

        private class UpdateOperationSimple:IUpdateOperation
        {
            public void Update(IShard shard, object obj)
            {
                shard.EstablishSession().Update(obj);
            }

            public void Merge(IShard shard, object obj)
            {
                shard.EstablishSession().Merge(obj);
            }
        }

        private class DeleteOperationSimple:IDeleteOperation
        {
            public void Delete(IShard shard, object obj)
            {
                shard.EstablishSession().Delete(obj);                
            }
        }

        private class DeleteOperationWithEntityName:IDeleteOperation
        {
            private string entityName;

            public DeleteOperationWithEntityName(string entityName)
            {
                this.entityName = entityName;
            }
            public void Delete(IShard shard, object obj)
            {
                shard.EstablishSession().Delete(entityName, obj);
            }
        }

        private class RefreshOperationSimple:IRefreshOperation
        {
            public void Refresh(IShard shard, object obj)
            {
                shard.EstablishSession().Refresh(obj);
            }
        }

        private class RefreshOperationWithLockMode:IRefreshOperation
        {
            private LockMode lockMode;

            public RefreshOperationWithLockMode(LockMode lockMode)
            {
                this.lockMode = lockMode;
            }


            public void Refresh(IShard shard, object obj)
            {
                shard.EstablishSession().Refresh(obj, lockMode);
            }
        }

        interface IRefreshOperation
        {
            void Refresh(IShard shard, object obj);
        }

        interface IDeleteOperation
        {
            void Delete(IShard shard, object obj);
        }

        interface IUpdateOperation
        {
            void Update(IShard shard, object obj);
            void Merge(IShard shard, object obj);
        }

        interface ISaveOrUpdateOperation
        {
            void SaveOrUpdate(IShard shard, object obj);
            void Merge(IShard shard, object obj);
        }

        #region Nested IShardOperation<object> types

        private class GetShardOperationByTypeAndId : IShardOperation<object>
        {

            private readonly System.Type clazz;
            private readonly object id;

            public GetShardOperationByTypeAndId(System.Type clazz, object id)
            {
                this.clazz = clazz;
                this.id = id;
            }

            #region IShardOperation<object> Members

            public object Execute(IShard shard)
            {
                return shard.EstablishSession().Get(clazz, id);
            }

            public string OperationName
            {
                get { return "get(System.Type clazz, ISerializable id)"; }
            }

            #endregion
        }


        private class GetShardOperationByTypeIdAndLockMode : IShardOperation<object>
        {

            private readonly System.Type clazz;
            private readonly ISerializable id;
            private readonly LockMode lockMode;

            public GetShardOperationByTypeIdAndLockMode(System.Type clazz, ISerializable id, LockMode lockMode)
            {
                this.clazz = clazz;
                this.id = id;
                this.lockMode = lockMode;
            }

            #region IShardOperation<object> Members

            public object Execute(IShard shard)
            {
                return shard.EstablishSession().Get(clazz, id,lockMode);
            }

            public string OperationName
            {
                get { return "get(System.Type clazz, ISerializable id, LockMode lockMode)"; }
            }

            #endregion
        }

        private class GetShardOperationByEntityName:IShardOperation<string>
        {
            private object obj;

            public GetShardOperationByEntityName(object obj)
            {
                this.obj = obj;
            }

            public string Execute(IShard shard)
            {
                return shard.EstablishSession().GetEntityName(obj);
            }

            public string OperationName
            {
                get{ return "getEntityName(object obj)"; }
            }
        }

        private class GetShardOperationByEntityNameIdAndLockMode:IShardOperation<object>
        {
            private string entityName;
            private ISerializable id;
            private LockMode lockMode;

            public GetShardOperationByEntityNameIdAndLockMode(string entityName, ISerializable id, LockMode lockMode)
            {
                this.entityName = entityName;
                this.id = id;
                this.lockMode = lockMode;                
            }

            public object Execute(IShard shard)
            {
                return shard.EstablishSession().Load(entityName, id);
            }

            public string OperationName
            {
                get { return "get(string entityName, ISerializableid, LockMode lockMode)"; }
            }
        }

        private class GetShardOperationByEntityNameAndId : IShardOperation<object>
        {
            private readonly string entityName;
            private readonly ISerializable id;            

            public GetShardOperationByEntityNameAndId(string entityName, ISerializable id)
            {
                this.entityName = entityName;
                this.id = id;
            }

            public object Execute(IShard shard)
            {
                return shard.EstablishSession().Get(entityName, id);
            }

            public string OperationName
            {
                get { return "get(string entityname, ISerializable id)"; }
            }
        }

        private class LockShardOperationByEntityNameObjectLockMode:IShardOperation<object>
        {
            private string entityName;
            private object obj;
            private LockMode lockMode;

            public LockShardOperationByEntityNameObjectLockMode(string entityName,object obj, LockMode lockMode)
            {
                this.entityName = entityName;
                this.obj = obj;
                this.lockMode = lockMode;
            }


            public object Execute(IShard shard)
            {
                shard.EstablishSession().Lock(entityName, obj, lockMode);
                return null;
            }

            public string OperationName
            {
                get { return "Lock(string entityName, object obj, LockMode lockMode)"; }
            }
        }

        private class LockShardOperationByObjectAndLockMode:IShardOperation<object>
        {
            
            private LockMode lockMode;
            private object obj;

            public LockShardOperationByObjectAndLockMode(object obj, LockMode lockMode)
            {
                this.lockMode = lockMode;
                this.obj = obj;
            }
            public object Execute(IShard shard)
            {
                shard.EstablishSession().Lock(obj, lockMode);
                return null;
            }

            public string OperationName
            {
                get { return "LockShardOperationByLockMode(object obj, LockMode lockMode)"; }
            }
        }

        private class CurrentLockModeShardOperation:IShardOperation<LockMode>
        {
            private object obj;

            public CurrentLockModeShardOperation(object obj)
            {
                this.obj = obj;
            }

            public LockMode Execute(IShard shard)
            {
                return shard.EstablishSession().GetCurrentLockMode(obj);
            }

            public string OperationName
            {
                get { return "GetCurrentLockMode(object obj)"; }
            }
        }

        #endregion
    }
}