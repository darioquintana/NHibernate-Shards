using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Iesi.Collections.Generic;
using log4net;
using NHibernate.Engine;
using NHibernate.Proxy;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Strategy.Selection;
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
		[ThreadStatic] private static ShardId currentSubgraphShardId;

		private readonly bool checkAllAssociatedObjectsForDifferentShards;
		private readonly Set<System.Type> classesWithoutTopLevelSaveSupport;
		private readonly ILog log = LogManager.GetLogger(typeof (ShardedSessionImpl));

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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Obtain a new ADO.NET connection.
		/// </summary>
		/// <remarks>
		/// This is used by applications which require long transactions
		/// </remarks>
		public void Reconnect()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Reconnect to the given ADO.NET connection.
		/// </summary>
		/// <remarks>This is used by applications which require long transactions</remarks>
		/// <param name="connection">An ADO.NET connection</param>
		public void Reconnect(IDbConnection connection)
		{
			throw new NotImplementedException();
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
				if (typeof (HibernateException).IsAssignableFrom(first.GetType()))
				{
					throw (HibernateException) first;
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
			get { return !closed; }
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
			List<ShardId> shardIds = SelectShardIdsFromShardResolutionStrategyData(
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
			throw new NotImplementedException();
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
			List<ShardId> shardIds = SelectShardIdsFromShardResolutionStrategyData(new
			                                                                       	ShardResolutionStrategyDataImpl(clazz, id));
			if (shardIds.Count == 1)
			{
				return shardIdsToShards[shardIds[0]].EstablishSession().Load(clazz, id);
			}
			else
			{
				Object result = Get(clazz, id);
				if (result == null)
				{
					shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(clazz.Name, id);
				}
				return result;
			}
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		public object Load(string entityName, object id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Read the persistent state associated with the given identifier into the given transient 
		/// instance.
		/// </summary>
		/// <param name="obj">An "empty" instance of the persistent class</param>
		/// <param name="id">A valid identifier of an existing persistent instance of the class</param>
		public void Load(object obj, object id)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Persist the given transient instance, using the given identifier.
		/// </summary>
		/// <param name="obj">A transient instance of a persistent class</param>
		/// <param name="id">An unused valid identifier</param>
		public void Save(object obj, object id)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		public void SaveOrUpdate(string entityName, object obj)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		public void Update(string entityName, object obj)
		{
			throw new NotImplementedException();
		}

		public object Merge(object obj)
		{
			throw new NotImplementedException();
		}

		public object Merge(string entityName, object obj)
		{
			throw new NotImplementedException();
		}

		public void Persist(object obj)
		{
			throw new NotImplementedException();
		}

		public void Persist(string entityName, object obj)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		public void Delete(string entityName, object obj)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Execute a query
		/// </summary>
		/// <param name="query">A query expressed in Hibernate's query language</param>
		/// <returns>A distinct list of instances</returns>
		/// <remarks>See <see cref="IQuery.List()"/> for implications of <c>cache</c> usage.</remarks>
		public IList Find(string query)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Delete all objects returned by the query.
		/// </summary>
		/// <param name="query">The query string</param>
		/// <returns>Returns the number of objects deleted.</returns>
		public int Delete(string query)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Obtain the specified lock level upon the given object.
		/// </summary>
		/// <param name="obj">A persistent instance</param>
		/// <param name="lockMode">The lock level</param>
		public void Lock(object obj, LockMode lockMode)
		{
			throw new NotImplementedException();
		}

		public void Lock(string entityName, object obj, LockMode lockMode)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Determine the current lock mode of the given object
		/// </summary>
		/// <param name="obj">A persistent instance</param>
		/// <returns>The current lock mode</returns>
		public LockMode GetCurrentLockMode(object obj)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Begin a transaction with the specified <c>isolationLevel</c>
		/// </summary>
		/// <param name="isolationLevel">Isolation level for the new transaction</param>
		/// <returns>A transaction instance having the specified isolation level</returns>
		public ITransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			throw new NotImplementedException();
		}

		public ICriteria CreateCriteria<T>() where T : class
		{
			throw new NotImplementedException();
		}

		public ICriteria CreateCriteria<T>(string alias) where T : class
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get the current Unit of Work and return the associated <c>ITransaction</c> object.
		/// </summary>
		public ITransaction Transaction
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Creates a new <c>Criteria</c> for the entity class.
		/// </summary>
		/// <param name="persistentClass">The class to Query</param>
		/// <returns>An ICriteria object</returns>
		public ICriteria CreateCriteria(System.Type persistentClass)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a new <c>Criteria</c> for the entity class with a specific alias
		/// </summary>
		/// <param name="persistentClass">The class to Query</param>
		/// <param name="alias">The alias of the entity</param>
		/// <returns>An ICriteria object</returns>
		public ICriteria CreateCriteria(System.Type persistentClass, string alias)
		{
			throw new NotImplementedException();
		}

		public ICriteria CreateCriteria(string entityName)
		{
			throw new NotImplementedException();
		}

		public ICriteria CreateCriteria(string entityName, string alias)
		{
			throw new NotImplementedException();
		}

		public IQueryOver<T> QueryOver<T>() where T : class
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Create a new instance of <c>Query</c> for the given query string
		/// </summary>
		/// <param name="queryString">A hibernate query string</param>
		/// <returns>The query</returns>
		public IQuery CreateQuery(string queryString)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Create a new instance of <c>Query</c> for the given collection and filter string
		/// </summary>
		/// <param name="collection">A persistent collection</param>
		/// <param name="queryString">A hibernate query</param>
		/// <returns>A query</returns>
		public IQuery CreateFilter(object collection, string queryString)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Create a new instance of <see cref="ISQLQuery" /> for the given SQL query string.
		/// </summary>
		/// <param name="queryString">a query expressed in SQL</param>
		/// <returns>An <see cref="ISQLQuery"/> from the SQL string</returns>
		public ISQLQuery CreateSQLQuery(string queryString)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Completely clear the session. Evict all loaded instances and cancel all pending
		/// saves, updates and deletions. Do not close open enumerables or instances of
		/// <c>ScrollableResults</c>.
		/// </summary>
		public void Clear()
		{
			throw new NotImplementedException();
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
			IShardOperation<Object> shardOp = new GetShardOperation();

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
			throw new NotImplementedException();
		}

		public object Get(string entityName, object id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Strongly-typed version of <see cref="ISession.Get(Type,object)" />
		/// </summary>
		public T Get<T>(object id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Strongly-typed version of <see cref="ISession.Get(Type,object,LockMode)" />
		/// </summary>
		public T Get<T>(object id, LockMode lockMode)
		{
			throw new NotImplementedException();
		}

		/// <summary> 
		/// Return the entity name for a persistent entity
		/// </summary>
		/// <param name="obj">a persistent entity</param>
		/// <returns> the entity name </returns>
		public string GetEntityName(object obj)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Enable the named filter for this current session.
		/// </summary>
		/// <param name="filterName">The name of the filter to be enabled.</param>
		/// <returns>The Filter instance representing the enabled fiter.</returns>
		public IFilter EnableFilter(string filterName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Retrieve a currently enabled filter by name.
		/// </summary>
		/// <param name="filterName">The name of the filter to be retrieved.</param>
		/// <returns>The Filter instance representing the enabled fiter.</returns>
		public IFilter GetEnabledFilter(string filterName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Disable the named filter for the current session.
		/// </summary>
		/// <param name="filterName">The name of the filter to be disabled.</param>
		public void DisableFilter(string filterName)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the batch size of the session
		/// </summary>
		/// <param name="batchSize"></param>
		/// <returns></returns>
		public ISession SetBatchSize(int batchSize)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		/// <summary>
		/// An <see cref="IMultiCriteria"/> that can return a list of all the results
		/// of all the criterias.
		/// </summary>
		/// <returns></returns>
		public IMultiCriteria CreateMultiCriteria()
		{
			throw new NotImplementedException();
		}

		public ISession GetSession(EntityMode entityMode)
		{
			throw new NotImplementedException();
		}

		public EntityMode ActiveEntityMode
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary> Get the statistics for this session.</summary>
		public ISessionStatistics Statistics
		{
			get { throw new NotImplementedException(); }
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
			get { throw new NotImplementedException(); }
		}

		#endregion

		private List<ShardId> SelectShardIdsFromShardResolutionStrategyData(ShardResolutionStrategyDataImpl srsd)
		{
			throw new NotImplementedException();
		}

		private object ApplyGetOperation(IShardOperation<object> shardOp, ShardResolutionStrategyDataImpl srsd)
		{
			throw new NotImplementedException();
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

		private IShard GetShardForObject(object obj, List<IShard> shardsToConsider)
		{
			//TODO: optimize this by keeping an identity map of objects to shardId

			foreach (IShard shard in shardsToConsider)
			{
				if (shard.Session != null && shard.Session.Contains(obj))
					return shard;
			}
			return null;
		}

		internal ShardId GetShardIdForObject(object obj, List<IShard> shardsToConsider)
		{
			//TODO Also, wouldn't it be faster to first see if there's just a single shard id mapped to the shard?

			IShard shard = GetShardForObject(obj, shardsToConsider);

			if (shard == null)
			{
				return null;
			}
			else if (shard.ShardIds.Count == 1)
			{
				IEnumerator<ShardId> iterator = shard.ShardIds.GetEnumerator();
				iterator.MoveNext();
				return iterator.Current;
			}
			else
			{
				System.Type clazz;
				if (obj is INHibernateProxy)
					clazz = ((INHibernateProxy) obj).HibernateLazyInitializer.PersistentClass;
				else
					clazz = obj.GetType();

				throw new NotImplementedException();
				//IIdentifierGenerator idGenerator = shard.SessionFactoryImplementor.GetIdentifierGenerator(clazz);

				//if (idGenerator is IShardEncodingIdentifierGenerator)
				//{
				//    return ((IShardEncodingIdentifierGenerator) idGenerator).ExtractShardId(GetIdentifier(obj));
				//}
				//else
				//{
				//    // TODO: also use shard resolution strategy if it returns only 1 shard; throw this error in config instead of here
				//    throw new HibernateException("Can not use virtual sharding with non-shard resolving id gen");
				//}
			}
		}

		private IDictionary<ShardId, IShard> BuildShardIdsToShardsMap()
		{
			throw new NotImplementedException();
		}

		private static List<IShard> BuildShardListFromSessionFactoryShardIdMap(
			IDictionary<ISessionFactoryImplementor, Set<ShardId>> sessionFactoryShardIdMap,
			bool checkAllAssociatedObjectsForDifferentShards,
			IShardIdResolver shardIdResolver,
			IInterceptor interceptor)
		{
			throw new NotImplementedException();
		}

		#region Nested type: GetShardOperation

		private class GetShardOperation : IShardOperation<object>
		{
			#region IShardOperation<object> Members

			public object Execute(IShard shard)
			{
				throw new NotImplementedException();
			}

			public string OperationName
			{
				get { throw new NotImplementedException(); }
			}

			#endregion
		}

		#endregion
	}
}