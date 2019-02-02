using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.AdoNet;
using NHibernate.Cache;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Engine.Query.Sql;
using NHibernate.Event;
using NHibernate.Hql;
using NHibernate.Impl;
using NHibernate.Linq;
using NHibernate.Loader.Custom;
using NHibernate.Metadata;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;
using NHibernate.Shards.Criteria;
using NHibernate.Shards.Engine;
using NHibernate.Shards.Query;
using NHibernate.Shards.Stat;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.Shards.Transaction;
using NHibernate.Shards.Util;
using NHibernate.Stat;
using NHibernate.Transaction;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Shards.Session
{
	using System.Diagnostics.CodeAnalysis;

	/// <summary>
	/// Concrete implementation of a ShardedSession, and also the central component of
	/// Hibernate Shards' internal implementation. This class exposes two interfaces;
	/// ShardedSession itself, to the application, and ShardedSessionImplementor, to
	/// other components of Hibernate Shards. This class is not threadsafe.
	/// </summary>
	public class ShardedSessionImpl : IShardedSession, IShardedSessionImplementor, IShardIdResolver
	{
		#region Static fields

		private static readonly Logger Log = new Logger(typeof(ShardedSessionImpl));
		private static readonly AsyncLocal<ShardId> currentSubgraphShardId = new AsyncLocal<ShardId>();

		#endregion

		#region Instance fields

		private readonly IShardedSessionFactoryImplementor shardedSessionFactory;
		private readonly IShardedSessionBuilderImplementor shardedSessionBuilder;
		private readonly IInterceptor interceptor;

		private readonly IDictionary<ShardId, IShard> shardsById = new Dictionary<ShardId, IShard>();
		private readonly IList<IShard> shards = new List<IShard>();

		// All sessions that have been opened  within the scope of this sharded session.
		private readonly IDictionary<IShard, ISession> establishedSessionsByShard = new Dictionary<IShard, ISession>();
		// Actions that are to be applied to newly opened sessions.
		private readonly IList<Action<ISession>> establishActions = new List<Action<ISession>>();

		// Partial ISessionImplementor implementation to intercept queries
		private ISessionImplementor sessionImpl;

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
		internal ShardedSessionImpl(
			IShardedSessionFactoryImplementor shardedSessionFactory,
			IShardedSessionBuilderImplementor shardedSessionBuilder)
		{
			Preconditions.CheckNotNull(shardedSessionFactory);

			this.shardedSessionFactory = shardedSessionFactory;
			this.shardedSessionBuilder = shardedSessionBuilder;
			this.interceptor = shardedSessionBuilder != null
				? BuildSessionInterceptor(this, shardedSessionBuilder.SessionInterceptor, shardedSessionFactory.CheckAllAssociatedObjectsForDifferentShards)
				: null;

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

		internal ShardedSessionImpl(
			ShardedSessionImpl rootSession,
			IShardedSessionBuilderImplementor shardedSessionBuilder)
		{
			Preconditions.CheckNotNull(rootSession);

			this.shardedSessionFactory = rootSession.shardedSessionFactory;
			this.shardedSessionBuilder = shardedSessionBuilder;
			this.interceptor = rootSession.interceptor;
			this.shards = rootSession.shards;
			this.shardsById = rootSession.shardsById;
		}

		#endregion

		public static ShardId CurrentSubgraphShardId
		{
			get { return currentSubgraphShardId.Value; }
			set { currentSubgraphShardId.Value = value; }
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
		/// The <paramref name="action"/> is performed immediately on all shard-local
		/// sessions that have already been established. It is also scheduled for
		/// execution when any new shard-local sessions are established within the
		/// scope of this sharded session.
		/// </remarks>
		public void ApplyActionToShards(Action<ISession> action)
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
			if (!this.establishedSessionsByShard.TryGetValue(shard, out var result))
			{
				result = this.shardedSessionBuilder != null 
					? this.shardedSessionBuilder.OpenSessionFor(shard, this.interceptor)
					: shard.SessionFactory.OpenSession();

				foreach (var action in establishActions)
				{
					action(result);
				}

				lock (this.transactionLock)
				{
					this.transaction?.Enlist(result);
				}

				establishedSessionsByShard.Add(shard, result);
			}
			return result;
		}

		private static IInterceptor BuildSessionInterceptor(ShardedSessionImpl shardedSession, IInterceptor interceptor, bool checkAllAssociatedObjectsForDifferentShards)
		{
			var defaultInterceptor = interceptor is IStatefulInterceptorFactory interceptorFactory
				? interceptorFactory.NewInstance()
				: interceptor;

			// cross shard association checks for updates are handled using interceptors
			if (!checkAllAssociatedObjectsForDifferentShards) return defaultInterceptor;

			var crossShardRelationshipDetector = new CrossShardRelationshipDetector(shardedSession);
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
		/// <param name="entityName">Entity name of <paramref name="obj"/>.</param>
		/// <param name="obj">the object for which we want the Session</param>
		/// <returns>
		/// the ShardId of the Shard with which this object is associated, or
		/// null if the object is not associated with a shard belonging to this
		/// ShardedSession
		/// </returns>
		public ShardId GetShardIdForObject(string entityName, object obj)
		{
			return TryGetShardIdForAttachedObject(entityName, obj, out var shardId)
				? shardId
				: null;
		}

		/// <summary>
		///  Gets the ShardId of the shard with which the object is associated.
		/// </summary>
		/// <param name="entityName">Entity name of <paramref name="obj"/>.</param>
		/// <param name="obj">the object for which we want the Session</param>
		/// <param name="result">Returns the <see cref="ShardId"/> of the shard with 
		/// which <paramref name="obj"/> is associated if this operation succeeds, 
		/// or <c>null</c> otherwise.</param>
		/// <returns>
		/// Returns <c>true</c> is <paramref name="obj"/> is associated with a shard
		/// that is within the scope of this sharded session.
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

			var key = ExtractKey(entityName, obj);
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
		/// <param name="operation">The operation to be performed on each shard.</param>
		public void Execute(IShardOperation operation)
		{
			this.shardedSessionFactory.ShardStrategy.ShardAccessStrategy.Apply(this.shards, operation);
		}

		/// <summary>
		/// Performs the specified asynchronous operation on the shards that are spanned by this session
		/// and aggregates the results from each shard into a single result.
		/// </summary>
		/// <param name="operation">The asynchronous operation to be performed on each shard.</param>
		/// <param name="cancellationToken">Cancellation token for this operation.</param>
		/// <returns>The aggregated operation result.</returns>
		public Task ExecuteAsync(IAsyncShardOperation operation, CancellationToken cancellationToken)
		{
			return this.shardedSessionFactory.ShardStrategy.ShardAccessStrategy.ApplyAsync(this.shards, operation, cancellationToken);
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
			return this.shardedSessionFactory.ShardStrategy.ShardAccessStrategy.Apply(this.shards, operation, exitStrategy);
		}

		/// <summary>
		/// Performs the specified asynchronous operation on the shards that are spanned by this session
		/// and aggregates the results from each shard into a single result.
		/// </summary>
		/// <typeparam name="T">Return value type.</typeparam>
		/// <param name="operation">The asynchronous operation to be performed on each shard.</param>
		/// <param name="exitStrategy">Strategy for collection and aggregation of
		/// operation results from the shards.</param>
		/// <param name="cancellationToken">Cancellation token for this operation.</param>
		/// <returns>The aggregated operation result.</returns>
		public Task<T> ExecuteAsync<T>(IAsyncShardOperation<T> operation, IExitStrategy<T> exitStrategy, CancellationToken cancellationToken)
		{
			return this.shardedSessionFactory.ShardStrategy.ShardAccessStrategy.ApplyAsync(this.shards, operation, exitStrategy, cancellationToken);
		}

		ShardedSharedSessionBuilder SessionWithOptions()
		{
			return new ShardedSharedSessionBuilder(this);
		}

		ISharedSessionBuilder ISession.SessionWithOptions()
		{
			return SessionWithOptions();
		}

		/// <inheritdoc />
		public void Flush()
		{
			foreach (var session in this.establishedSessionsByShard.Values)
			{
				session.Flush();
			}
		}

		/// <inheritdoc />
		public async Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			foreach (var session in this.establishedSessionsByShard.Values)
			{
				await session.FlushAsync(cancellationToken);
			}
		}

		/// <inheritdoc />
		public FlushMode FlushMode
		{
			get { return AnySession.FlushMode; }
			set { ApplyActionToShards(s => s.FlushMode = value); }
		}

		/// <inheritdoc />
		public CacheMode CacheMode
		{
			get { return AnySession.CacheMode; }
			set { ApplyActionToShards(s => s.CacheMode = value); }
		}

		/// <inheritdoc />
		public ISessionFactory SessionFactory
		{
			get { return shardedSessionFactory; }
		}

		/// <inheritdoc />
		public DbConnection Connection
		{
			get { throw new InvalidOperationException("On Shards this is deprecated"); }
		}

		/// <inheritdoc />
		public DbConnection Disconnect()
		{
			foreach (var session in establishedSessionsByShard.Values)
			{
				session.Disconnect();
			}

			// We do not allow application-supplied connections, so we can always return null
			return null;
		}

		/// <inheritdoc />
		public void Reconnect()
		{
			foreach (var session in establishedSessionsByShard.Values)
			{
				session.Reconnect();
			}
		}

		/// <inheritdoc />
		public void Reconnect(DbConnection connection)
		{
			throw new NotSupportedException("Cannot reconnect a sharded session");
		}

		/// <inheritdoc />
		public DbConnection Close()
		{
			Exception firstException = null;

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

			closed = true;

			if (firstException != null) throw firstException;
			return null;
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public bool IsDirty()
		{
			foreach (var session in establishedSessionsByShard.Values)
			{
				if (session.IsDirty()) return true;
			}
			return false;
		}

		/// <inheritdoc />
		public async Task<bool> IsDirtyAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			foreach (var session in establishedSessionsByShard.Values)
			{
				if (await session.IsDirtyAsync(cancellationToken)) return true;
			}
			return false;
		}

		/// <inheritdoc />
		public bool DefaultReadOnly
		{
			get { return this.AnySession.DefaultReadOnly; }
			set { ApplyActionToShards(s => { s.DefaultReadOnly = value; }); }
		}

		/// <inheritdoc />
		public bool IsReadOnly(object entityOrProxy)
		{
			return GetSessionForAttachedObject(entityOrProxy).IsReadOnly(entityOrProxy);
		}

		/// <inheritdoc />
		public void SetReadOnly(object entityOrProxy, bool readOnly)
		{
			GetSessionForAttachedObject(entityOrProxy).SetReadOnly(entityOrProxy, readOnly);
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public bool Contains(object obj)
		{
			foreach (var session in establishedSessionsByShard.Values)
			{
				if (session.Contains(obj)) return true;
			}
			return false;
		}

		/// <inheritdoc />
		public void Evict(object obj)
		{
			foreach (var session in establishedSessionsByShard.Values)
			{
				session.Evict(obj);
			}
		}

		/// <inheritdoc />
		public async Task EvictAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			foreach (var session in establishedSessionsByShard.Values)
			{
				await session.EvictAsync(obj, cancellationToken);
			}
		}


		#region Load

		/// <inheritdoc />
		public object Load(System.Type clazz, object id, LockMode lockMode)
		{
			var key = new ShardedEntityKey(GuessEntityName(clazz), id);
			return Load(key, null);
		}

		/// <inheritdoc />
		public Task<object> LoadAsync(System.Type theType, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(GuessEntityName(theType), id);
			return LoadAsync<object>(key, null, cancellationToken);
		}

		/// <inheritdoc />
		public object Load(string entityName, object id, LockMode lockMode)
		{
			var key = new ShardedEntityKey(entityName, id);
			return Load(key, lockMode);
		}

		/// <inheritdoc />
		public Task<object> LoadAsync(string entityName, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(entityName, id);
			return LoadAsync<object>(key, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public object Load(System.Type clazz, object id)
		{
			var key = new ShardedEntityKey(GuessEntityName(clazz), id);
			return Load(key, null);
		}

		/// <inheritdoc />
		public Task<object> LoadAsync(System.Type theType, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(GuessEntityName(theType), id);
			return LoadAsync<object>(key, null, cancellationToken);
		}

		/// <inheritdoc />
		public T Load<T>(object id, LockMode lockMode)
		{
			var key = new ShardedEntityKey(GuessEntityName(typeof(T)), id);
			return (T)Load(key, lockMode);
		}

		/// <inheritdoc />
		public Task<T> LoadAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(GuessEntityName(typeof(T)), id);
			return LoadAsync<T>(key, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public T Load<T>(object id)
		{
			var key = new ShardedEntityKey(GuessEntityName(typeof(T)), id);
			return (T)Load(key, null);
		}

		/// <inheritdoc />
		public Task<T> LoadAsync<T>(object id, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(GuessEntityName(typeof(T)), id);
			return LoadAsync<T>(key, null, cancellationToken);
		}

		/// <inheritdoc />
		public object Load(string entityName, object id)
		{
			var key = new ShardedEntityKey(entityName, id);
			return Load(key, null);
		}

		/// <inheritdoc />
		public Task<object> LoadAsync(string entityName, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(entityName, id);
			return LoadAsync<object>(key, null, cancellationToken);
		}

		private object Load(ShardedEntityKey key, LockMode lockMode)
		{
			if (TryResolveToSingleShard(key, out var shard))
			{
				return shard.EstablishSession().Load(key.EntityName, key.Id, lockMode);
			}

			if (!TryGet(key, lockMode, out var persistent))
			{
				this.shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(key.EntityName, key.Id);
			}
			return persistent.Value;
		}

		[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
		private async Task<T> LoadAsync<T>(ShardedEntityKey key, LockMode lockMode, CancellationToken cancellationToken)
		{
			if (TryResolveToSingleShard(key, out var shard))
			{
				return (T)await shard.EstablishSession().LoadAsync(key.EntityName, key.Id, lockMode, cancellationToken);
			}

			var persistent = await TryGetAsync(key, lockMode, cancellationToken);
			if (persistent == null)
			{
				this.shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(key.EntityName, key.Id);
			}
			return (T)persistent.Value;
		}

		/// <inheritdoc />
		public void Load(object obj, object id)
		{
			var key = new ShardedEntityKey(GuessEntityName(obj), id);
			Load(obj, key);
		}

		/// <inheritdoc />
		public Task LoadAsync(object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(GuessEntityName(obj), id);
			return LoadAsync(obj, key, cancellationToken);
		}

		private void Load(object entity, ShardedEntityKey key)
		{
			if (TryResolveToSingleShard(key, out var shard))
			{
				shard.EstablishSession().Load(entity, key);
				return;
			}

			if (TryGet(key, null, out var persistent))
			{
				Evict(persistent.Value);
				persistent.Shard.EstablishSession().Load(entity, key);
			}

			shardedSessionFactory.EntityNotFoundDelegate.HandleEntityNotFound(key.EntityName, key);
		}

		private async Task LoadAsync(object entity, ShardedEntityKey key, CancellationToken cancellationToken)
		{
			if (TryResolveToSingleShard(key, out var shard))
			{
				await shard.EstablishSession().LoadAsync(entity, key, cancellationToken);
			}

			var persistent = await TryGetAsync(key, null, cancellationToken);
			if (persistent != null)
			{
				await EvictAsync(persistent.Value, cancellationToken);
				await persistent.Shard.EstablishSession().LoadAsync(entity, key, cancellationToken);
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
			Replicate(obj, ExtractKey(null, obj), replicationMode);
		}

		public Task ReplicateAsync(object obj, ReplicationMode replicationMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return ReplicateAsync(obj, ExtractKey(null, obj), replicationMode, cancellationToken);
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

		public Task ReplicateAsync(string entityName, object obj, ReplicationMode replicationMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return ReplicateAsync(obj, ExtractKey(entityName, obj), replicationMode, cancellationToken);
		}

		private void Replicate(object entity, ShardedEntityKey key, ReplicationMode replicationMode)
		{
			if (TryResolveToSingleShardId(key, out var shardId))
			{
				// Attached object
				CurrentSubgraphShardId = shardId;
				shardsById[shardId].EstablishSession().Replicate(key.EntityName, entity, replicationMode);
				return;
			}

			if (TryGet(key, null, out var persistent))
			{
				// Detached object
				Evict(persistent.Value);
				persistent.Shard.EstablishSession().Replicate(key.EntityName, entity, replicationMode);
			}
			else
			{
				// Transient object
				CurrentSubgraphShardId = shardId = SelectShardIdForNewEntity(key.EntityName, entity);
				shardsById[shardId].EstablishSession().Replicate(key.EntityName, entity, replicationMode);
			}
		}

		private async Task ReplicateAsync(object entity, ShardedEntityKey key, ReplicationMode replicationMode, CancellationToken cancellationToken)
		{
			if (TryResolveToSingleShardId(key, out var shardId))
			{
				// Attached object
				CurrentSubgraphShardId = shardId;
				await shardsById[shardId].EstablishSession().ReplicateAsync(key.EntityName, entity, replicationMode, cancellationToken);
				return;
			}

			var persistent = await TryGetAsync(key, null, cancellationToken);
			if (persistent != null)
			{
				// Detached object
				await EvictAsync(persistent.Value, cancellationToken);
				await persistent.Shard.EstablishSession().ReplicateAsync(key.EntityName, entity, replicationMode, cancellationToken);
			}
			else
			{
				// Transient object
				CurrentSubgraphShardId = shardId = SelectShardIdForNewEntity(key.EntityName, entity);
				await shardsById[shardId].EstablishSession().ReplicateAsync(key.EntityName, entity, replicationMode, cancellationToken);
			}
		}

		#endregion

		private ShardedEntityKey ExtractKey(string entityName, object entity)
		{
			Preconditions.CheckNotNull(entity);
			if (entityName == null) entityName = GuessEntityName(entity);

			var classMetadata = shardedSessionFactory.GetClassMetadata(entityName);
			var id = classMetadata.GetIdentifier(entity);
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

		public Task<object> SaveAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return SaveAsync(null, obj, cancellationToken);
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

			if (!TryGetShardIdForAttachedObject(entityName, obj, out var shardId))
			{
				// Transient object
				shardId = SelectShardIdForNewEntity(entityName, obj);
				Preconditions.CheckNotNull(shardId);
			}

			// Attached object
			CurrentSubgraphShardId = shardId;
			Log.Debug($"Saving object of type '{entityName}' to shard {shardId}");
			return this.shardsById[shardId].EstablishSession().Save(entityName, obj);
		}

		public Task<object> SaveAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (!TryGetShardIdForAttachedObject(entityName, obj, out var shardId))
			{
				// Transient object
				shardId = SelectShardIdForNewEntity(entityName, obj);
				Preconditions.CheckNotNull(shardId);
			}

			// Attached object
			CurrentSubgraphShardId = shardId;
			Log.Debug($"Saving object of type '{entityName}' to shard {shardId}");
			return this.shardsById[shardId].EstablishSession().SaveAsync(entityName, obj, cancellationToken);
		}

		/// <summary>
		/// Persist the given transient instance, using the given identifier.
		/// </summary>
		/// <param name="obj">A transient instance of a persistent class</param>
		/// <param name="id">An unused valid identifier</param>
		public void Save(object obj, object id)
		{
			Save(null, obj, id);
		}

		public Task SaveAsync(object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return SaveAsync(null, obj, id, cancellationToken);
		}

		/// <summary>
		/// Persist the given transient instance, using the given identifier.
		/// </summary>
		/// <param name="entityName">The Entity name.</param>
		/// <param name="obj">A transient instance of a persistent class</param>
		/// <param name="id">An unused valid identifier</param>
		public void Save(string entityName, object obj, object id)
		{
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (!TryGetShardIdForAttachedObject(entityName, obj, out var shardId))
			{
				// Transient object
				shardId = SelectShardIdForNewEntity(entityName, obj);
				Preconditions.CheckNotNull(shardId);
			}

			// Attached object
			CurrentSubgraphShardId = shardId;
			Log.Debug($"Saving object of type '{entityName}' to shard {shardId}");
			this.shardsById[shardId].EstablishSession().Save(obj, id);
		}

		public Task SaveAsync(string entityName, object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (!TryGetShardIdForAttachedObject(entityName, obj, out var shardId))
			{
				// Transient object
				shardId = SelectShardIdForNewEntity(entityName, obj);
				Preconditions.CheckNotNull(shardId);
			}

			// Attached object
			CurrentSubgraphShardId = shardId;
			Log.Debug($"Saving object of type '{entityName}' to shard {shardId}");
			return this.shardsById[shardId].EstablishSession().SaveAsync(obj, id, cancellationToken);
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
			if (!TryGetShardIdOfAssociatedObject(entityName, entity, out var shardId))
			{
				CheckForUnsupportedTopLevelSave(entity.GetType());
				shardId = this.shardedSessionFactory.ShardStrategy.ShardSelectionStrategy.SelectShardIdForNewObject(entity);
			}

			// lock has been requested but shard has not yet been selected - lock it in
			if (lockedShard) lockedShardId = shardId;

			Log.Debug($"Selected shard '{shardId.Id}' for object of type '{entityName}'");
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
		private void CheckForUnsupportedTopLevelSave(System.Type entityClass)
		{
			if (this.shardedSessionFactory.IsClassWithTopLevelSaveSupport(entityClass))
			{
				string msg = $"Attempt to save object of type {entityClass.Name} as top-level object";
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
				if (this.shardedSessionFactory.CheckAllAssociatedObjectsForDifferentShards)
				{
					while (shardIdEnum.MoveNext())
					{
						ThrowIfConflictingShardId(entityName, result, shardIdEnum.Current);
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
			foreach (var pair in GetEntityAssociations(classMetaData, entity))
			{
				if (pair.Key.IsCollectionType)
				{
					/*
					 * collection types are more expensive to evaluate (might involve
					 * lazy-loading the contents of the collection from the db), so
					 * let's hold off until the end on the chance that we can fail
					 * quickly.
					 */
					if (pair.Value is ICollection collection)
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

		private static void ThrowIfConflictingShardId(string entityName, ShardId shardId, KeyValuePair<string, ShardId> associatedShardId)
		{
			if (!associatedShardId.Value.Equals(shardId))
			{
				var message = $"Object of entity type '{entityName}' is on shard '{shardId}' but an associated object of type '{associatedShardId.Key}' is on shard '{associatedShardId.Value}'.";
				Log.Error(message);
				throw new CrossShardAssociationException(message);
			}
		}

		private static IEnumerable<KeyValuePair<IAssociationType, object>> GetEntityAssociations(IClassMetadata classMetadata, object entity)
		{
			var propertyTypes = classMetadata.PropertyTypes;
			var propertyValues = classMetadata.GetPropertyValues(entity);
			return GetEntityAssociations(propertyTypes, propertyValues);

		}

		private static IEnumerable<KeyValuePair<IAssociationType, object>> GetEntityAssociations(IType[] propertyTypes, object[] propertyValues)
		{
			// we assume types and current state are the same length
			Preconditions.CheckState(propertyTypes.Length == propertyValues.Length);

			for (int i = 0; i < propertyTypes.Length; i++)
			{
				if (propertyTypes[i] != null &&
					propertyValues[i] != null &&
					propertyTypes[i].IsAssociationType &&
					propertyTypes[i].IsEntityType)
				{
					yield return new KeyValuePair<IAssociationType, object>((IAssociationType)propertyTypes[i], propertyValues[i]);
				}
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

		public Task SaveOrUpdateAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return SaveOrUpdateAsync(null, obj, cancellationToken);
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
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// attached object
				shard.EstablishSession().SaveOrUpdate(entityName, obj);
				return;
			}

			var key = new ShardedEntityKey(entityName, obj);
			if (!key.IsNull)
			{
				// detached object
				if (TryResolveToSingleShard(key, out shard))
				{
					shard.EstablishSession().SaveOrUpdate(entityName, obj);
					return;
				}

				/*
				 * Too bad, we've got a detached object that could be on more than 1 shard.
				 * The only safe way to handle this is to try and lookup the object, and if
				 * it exists, do a merge, and if it doesn't, do a save.
				 */
				if (TryGet(key, null, out var persistent))
				{
					persistent.Shard.EstablishSession().Merge(entityName, obj);
					return;
				}
			}

			shard.EstablishSession().Save(entityName, obj);
		}

		public async Task SaveOrUpdateAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// attached object
				await shard.EstablishSession().SaveOrUpdateAsync(entityName, obj, cancellationToken);
				return;
			}

			var key = new ShardedEntityKey(entityName, obj);
			if (!key.IsNull)
			{
				// detached object
				if (TryResolveToSingleShard(key, out shard))
				{
					await shard.EstablishSession().SaveOrUpdateAsync(entityName, obj, cancellationToken);
					return;
				}

				/*
				 * Too bad, we've got a detached object that could be on more than 1 shard.
				 * The only safe way to handle this is to try and lookup the object, and if
				 * it exists, do a merge, and if it doesn't, do a save.
				 */
				var persistent = await TryGetAsync(key, null, cancellationToken);
				if (persistent != null)
				{
					await persistent.Shard.EstablishSession().MergeAsync(entityName, obj, cancellationToken);
					return;
				}
			}

			await shard.EstablishSession().SaveAsync(entityName, obj, cancellationToken);
		}

		public void SaveOrUpdate(string entityName, object obj, object id)
		{
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// attached object
				shard.EstablishSession().SaveOrUpdate(entityName, obj);
				return;
			}

			var key = new ShardedEntityKey(entityName, id);
			if (!key.IsNull)
			{
				// detached object
				if (TryResolveToSingleShard(key, out shard))
				{
					shard.EstablishSession().SaveOrUpdate(entityName, obj);
					return;
				}

				/*
				 * Too bad, we've got a detached object that could be on more than 1 shard.
				 * The only safe way to handle this is to try and lookup the object, and if
				 * it exists, do a merge, and if it doesn't, do a save.
				 */
				if (TryGet(key, null, out var persistent))
				{
					persistent.Shard.EstablishSession().Merge(entityName, obj);
					return;
				}
			}

			shard.EstablishSession().Save(entityName, obj);
		}

		public async Task SaveOrUpdateAsync(string entityName, object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// attached object
				await shard.EstablishSession().SaveOrUpdateAsync(entityName, obj, cancellationToken);
				return;
			}

			var key = new ShardedEntityKey(entityName, id);
			if (!key.IsNull)
			{
				// detached object
				if (TryResolveToSingleShard(key, out shard))
				{
					await shard.EstablishSession().SaveOrUpdateAsync(entityName, obj, cancellationToken);
					return;
				}

				/*
				 * Too bad, we've got a detached object that could be on more than 1 shard.
				 * The only safe way to handle this is to try and lookup the object, and if
				 * it exists, do a merge, and if it doesn't, do a save.
				 */
				var persistent = await TryGetAsync(key, null, cancellationToken);
				if (persistent != null)
				{
					await persistent.Shard.EstablishSession().MergeAsync(entityName, obj, cancellationToken);
					return;
				}
			}

			await shard.EstablishSession().SaveAsync(entityName, obj, cancellationToken);
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

		public Task UpdateAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return UpdateAsync(null, obj, cancellationToken);
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
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (TryGetShardForAttachedEntity(obj, out var shard))
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

				/*
				 * Too bad, we've got a detached object that could be on more than 1 shard.
				 * The only safe way to perform the update is to load the object and then
				 * do a merge.
				 */
				if (TryGet(key, null, out var persistent))
				{
					persistent.Shard.EstablishSession().Merge(entityName, obj);
					return;
				}
			}

			/*
			 * This is an error condition.  In order to provide the same behavior
			 * as a non-sharded session we're just going to dispatch the update
			 * to a random shard (we know it will fail because either we don't have
			 * an id or the lookup returned).
			 */
			AnySession.Update(entityName, obj);
			// this call may succeed but the commit will fail
		}

		public async Task UpdateAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// attached object
				await shard.EstablishSession().UpdateAsync(entityName, obj, cancellationToken);
				return;
			}

			var key = ExtractKey(entityName, obj);
			if (!key.IsNull)
			{
				// detached object
				if (TryResolveToSingleShard(key, out shard))
				{
					await shard.EstablishSession().UpdateAsync(entityName, obj, cancellationToken);
					return;
				}

				/*
				 * Too bad, we've got a detached object that could be on more than 1 shard.
				 * The only safe way to perform the update is to load the object and then
				 * do a merge.
				 */
				var persistent = await TryGetAsync(key, null, cancellationToken);
				if (persistent != null)
				{
					await persistent.Shard.EstablishSession().MergeAsync(entityName, obj, cancellationToken);
					return;
				}
			}

			/*
			 * This is an error condition.  In order to provide the same behavior
			 * as a non-sharded session we're just going to dispatch the update
			 * to a random shard (we know it will fail because either we don't have
			 * an id or the lookup returned).
			 */
			await AnySession.UpdateAsync(entityName, obj, cancellationToken);
			// this call may succeed but the commit will fail
		}

		/// <summary>
		/// Update the persistent instance associated with the given identifier.
		/// </summary>
		/// <param name="obj">a detached instance containing updated state </param>
		/// <param name="id">Identifier of persistent instance</param>
		/// <remarks>
		/// If there is a persistent instance with the same identifier, an exception 
		/// is thrown. This operation cascades to associated instances if the association 
		/// is mapped with <tt>cascade="save-update"</tt>.
		/// </remarks>
		public void Update(object obj, object id)
		{
			Update(null, obj, id);
		}

		public Task UpdateAsync(object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return UpdateAsync(null, obj, id, cancellationToken);
		}

		/// <summary>
		/// Update the persistent instance associated with the given identifier.
		/// </summary>
		/// <param name="entityName">The Entity name.</param>
		/// <param name="obj">a detached instance containing updated state </param>
		/// <param name="id">Identifier of persistent instance</param>
		/// <remarks>
		/// If there is a persistent instance with the same identifier, an exception 
		/// is thrown. This operation cascades to associated instances if the association 
		/// is mapped with <tt>cascade="save-update"</tt>.
		/// </remarks>
		public void Update(string entityName, object obj, object id)
		{
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// attached object
				shard.EstablishSession().Update(obj, id);
				return;
			}

			var key = new ShardedEntityKey(entityName, id);
			if (!key.IsNull)
			{
				if (TryResolveToSingleShard(key, out shard))
				{
					shard.EstablishSession().Update(obj, id);
					return;
				}

				/*
				* Too bad, we've got a detached object that could be on more than 1 shard.
				* The only safe way to perform the update is to load the object and then
				* do a merge.
				*/
				if (TryGet(key, null, out var persistent))
				{
					persistent.Shard.EstablishSession().Merge(entityName, obj);
					return;
				}
			}

			/*
			 * This is an error condition.  In order to provide the same behavior
			 * as a non-sharded session we're just going to dispatch the update
			 * to a random shard (we know it will fail because either we don't have
			 * an id or the lookup returned another persistent entity with the same id).
			 */
			AnySession.Update(obj, id);
			// this call may succeed but the commit will fail
		}

		public async Task UpdateAsync(string entityName, object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// attached object
				await shard.EstablishSession().UpdateAsync(obj, id, cancellationToken);
				return;
			}

			var key = new ShardedEntityKey(entityName, id);
			if (!key.IsNull)
			{
				if (TryResolveToSingleShard(key, out shard))
				{
					await shard.EstablishSession().UpdateAsync(obj, id, cancellationToken);
					return;
				}

				/*
				* Too bad, we've got a detached object that could be on more than 1 shard.
				* The only safe way to perform the update is to load the object and then
				* do a merge.
				*/
				var persistent = await TryGetAsync(key, null, cancellationToken);
				if (persistent != null)
				{
					await persistent.Shard.EstablishSession().MergeAsync(entityName, obj, cancellationToken);
					return;
				}
			}

			/*
			 * This is an error condition.  In order to provide the same behavior
			 * as a non-sharded session we're just going to dispatch the update
			 * to a random shard (we know it will fail because either we don't have
			 * an id or the lookup returned another persistent entity with the same id).
			 */
			await AnySession.UpdateAsync(obj, id, cancellationToken);
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

		public Task<object> MergeAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return MergeAsync(null, obj, cancellationToken);
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

			if (TryResolveToSingleShardId(key, out var shardId))
			{
				CurrentSubgraphShardId = shardId;
				return shardsById[shardId].EstablishSession().Merge(entityName, obj);
			}

			if (TryGet(key, null, out var persistent))
			{
				return persistent.Shard.EstablishSession().Merge(entityName, obj);
			}

			CurrentSubgraphShardId = shardId = SelectShardIdForNewEntity(key.EntityName, obj);
			return shardsById[shardId].EstablishSession().Merge(entityName, obj);
		}

		public async Task<object> MergeAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = ExtractKey(entityName, obj);

			if (TryResolveToSingleShardId(key, out var shardId))
			{
				CurrentSubgraphShardId = shardId;
				return await shardsById[shardId].EstablishSession().MergeAsync(entityName, obj, cancellationToken);
			}

			var persistent = await TryGetAsync(key, null, cancellationToken);
			if (persistent != null)
			{
				return await persistent.Shard.EstablishSession().MergeAsync(entityName, obj, cancellationToken);
			}

			CurrentSubgraphShardId = shardId = SelectShardIdForNewEntity(key.EntityName, obj);
			return await shardsById[shardId].EstablishSession().MergeAsync(entityName, obj, cancellationToken);
		}

		public T Merge<T>(T entity) where T : class
		{
			return (T)Merge(null, (object)entity);
		}

		public Task<T> MergeAsync<T>(T entity, CancellationToken cancellationToken = new CancellationToken()) where T : class
		{
			return MergeAsync(null, entity, cancellationToken);
		}

		public T Merge<T>(string entityName, T entity) where T : class
		{
			return (T)Merge(entityName, (object)entity);
		}

		public async Task<T> MergeAsync<T>(string entityName, T entity, CancellationToken cancellationToken = new CancellationToken()) where T : class
		{
			return (T)await MergeAsync(entityName, (object)entity, cancellationToken);
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

		public Task PersistAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return PersistAsync(null, obj, cancellationToken);
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

			if (!TryGetShardIdForAttachedObject(entityName, obj, out var shardId))
			{
				// Detached object
				shardId = SelectShardIdForNewEntity(entityName, obj);
			}

			CurrentSubgraphShardId = shardId;
			Log.Debug($"Persisting object of type '{entityName}' to shard '{shardId}'");
			shardsById[shardId].EstablishSession().Persist(entityName, obj);
		}

		public Task PersistAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			Preconditions.CheckNotNull(obj);
			if (entityName == null) entityName = GuessEntityName(obj);

			if (!TryGetShardIdForAttachedObject(entityName, obj, out var shardId))
			{
				// Detached object
				shardId = SelectShardIdForNewEntity(entityName, obj);
			}

			CurrentSubgraphShardId = shardId;
			Log.Debug($"Persisting object of type '{entityName}' to shard '{shardId}'");
			return shardsById[shardId].EstablishSession().PersistAsync(entityName, obj, cancellationToken);
		}

		#endregion

		#region Delete

		/// <summary>
		/// Remove a persistent instance from the data store.
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

		public Task DeleteAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return DeleteAsync(null, obj, cancellationToken);
		}

		public void Delete(string entityName, object obj)
		{
			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// attached object
				shard.EstablishSession().Delete(entityName, obj);
				return;
			}

			/*
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

				/*
				 * Too bad, we've got a detached object that could be on more than 1 shard.
				 * The only safe way to perform the delete is to load the object before
				 * deleting.
				 */
				if (TryGet(key, null, out var persistent))
				{
					persistent.Shard.EstablishSession().Delete(entityName, persistent.Value);
				}
			}
		}

		public async Task DeleteAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// attached object
				await shard.EstablishSession().DeleteAsync(entityName, obj, cancellationToken);
				return;
			}

			/*
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
					await shard.EstablishSession().DeleteAsync(entityName, obj, cancellationToken);
					return;
				}

				/*
				 * Too bad, we've got a detached object that could be on more than 1 shard.
				 * The only safe way to perform the delete is to load the object before
				 * deleting.
				 */
				var persistent = await TryGetAsync(key, null, cancellationToken);
				if (persistent != null)
				{
					await persistent.Shard.EstablishSession().DeleteAsync(entityName, persistent.Value, cancellationToken);
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

		public Task<int> DeleteAsync(string query, CancellationToken cancellationToken = new CancellationToken())
		{
			return ExecuteAsync(
				new DeleteAsyncOperation((s, ct) => s.DeleteAsync(query, ct)),
				new ExecuteUpdateExitStrategy(),
				cancellationToken);
		}

		/// <summary>
		/// Delete all objects returned by the query.
		/// </summary>
		/// <param name="query">The query string</param>
		/// <param name="value">A value to be written to a "?" placeholder in the query</param>
		/// <param name="type">The hibernate type of value.</param>
		/// <returns>The number of instances deleted</returns>
		public int Delete(string query, object value, IType type)
		{
			return Execute(
				new DeleteOperation(s => s.Delete(query, value, type)),
				new ExecuteUpdateExitStrategy());
		}

		public Task<int> DeleteAsync(string query, object value, IType type, CancellationToken cancellationToken = new CancellationToken())
		{
			return ExecuteAsync(
				new DeleteAsyncOperation((s, ct) => s.DeleteAsync(query, value, type, ct)),
				new ExecuteUpdateExitStrategy(),
				cancellationToken);
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

		public Task<int> DeleteAsync(string query, object[] values, IType[] types,
			CancellationToken cancellationToken = new CancellationToken())
		{
			return ExecuteAsync(
				new DeleteAsyncOperation((s, ct) => s.DeleteAsync(query, values, types, ct)),
				new ExecuteUpdateExitStrategy(),
				cancellationToken);
		}

		private class DeleteOperation : IShardOperation<int>
		{
			private readonly Func<ISession, int> deleteAction;

			public DeleteOperation(Func<ISession, int> deleteAction)
			{
				this.deleteAction = deleteAction;
			}

			public Func<int> Prepare(IShard shard)
			{
				// NOTE: Establish action is not thread-safe and therefore must not be performed by returned delegate.
				var session = shard.EstablishSession();
				return () => deleteAction(session);
			}

			public string OperationName
			{
				get { return "delete(query)"; }
			}
		}

		private class DeleteAsyncOperation : IAsyncShardOperation<int>
		{
			private readonly Func<ISession, CancellationToken, Task<int>> deleteAction;

			public DeleteAsyncOperation(Func<ISession, CancellationToken, Task<int>> deleteAction)
			{
				this.deleteAction = deleteAction;
			}

			public Func<CancellationToken, Task<int>> PrepareAsync(IShard shard)
			{
				var session = shard.EstablishSession();
				return ct => deleteAction(session, ct);
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
			return GetSessionForAttachedObject(obj).GetCurrentLockMode(obj);
		}

		/// <summary>
		/// Obtain the specified lock level upon the given object.
		/// </summary>
		/// <param name="obj">A persistent instance</param>
		/// <param name="lockMode">The lock level</param>
		public void Lock(object obj, LockMode lockMode)
		{
			GetSessionForAttachedObject(obj).Lock(obj, lockMode);
		}

		public Task LockAsync(object obj, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return GetSessionForAttachedObject(obj).LockAsync(obj, lockMode, cancellationToken);
		}

		public void Lock(string entityName, object obj, LockMode lockMode)
		{
			GetSessionForAttachedObject(obj).Lock(entityName, obj, lockMode);
		}

		public Task LockAsync(string entityName, object obj, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return GetSessionForAttachedObject(obj).LockAsync(entityName, obj, lockMode, cancellationToken);
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
			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// Attached object
				shard.EstablishSession().Refresh(obj);
			}
		}

		public Task RefreshAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// Attached object
				return shard.EstablishSession().RefreshAsync(obj, cancellationToken);
			}

			return Task.CompletedTask;
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
			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// Attached object
				shard.EstablishSession().Refresh(obj, lockMode);
			}
		}

		public Task RefreshAsync(object obj, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			if (TryGetShardForAttachedEntity(obj, out var shard))
			{
				// Attached object
				return shard.EstablishSession().RefreshAsync(obj, lockMode, cancellationToken);
			}

			return Task.CompletedTask;
		}

		#endregion

		#region Transaction

		/// <inheritdoc />
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

		/// <inheritdoc />
		public ITransaction BeginTransaction()
		{
			return BeginTransaction(IsolationLevel.Unspecified);
		}

		/// <inheritdoc />
		public ITransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			ErrorIfClosed();

			var result = Transaction;
			result.Begin(isolationLevel);
			return result;
		}

		/// <inheritdoc />
		public void JoinTransaction()
		{
			foreach (var session in this.establishedSessionsByShard.Values)
			{
				session.JoinTransaction();
			}
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
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
			return new ShardedCriteriaImpl(this, GuessEntityName(persistentClass), s => s.CreateCriteria(persistentClass));
		}

		/// <summary>
		/// Creates a new <c>Criteria</c> for the entity class with a specific alias
		/// </summary>
		/// <param name="persistentClass">The class to Query</param>
		/// <param name="alias">The alias of the entity</param>
		/// <returns>An ICriteria object</returns>
		public ICriteria CreateCriteria(System.Type persistentClass, string alias)
		{
			return new ShardedCriteriaImpl(this, GuessEntityName(persistentClass), s => s.CreateCriteria(persistentClass, alias));
		}

		public ICriteria CreateCriteria(string entityName)
		{
			return new ShardedCriteriaImpl(this, entityName, s => s.CreateCriteria(entityName));
		}

		public ICriteria CreateCriteria(string entityName, string alias)
		{
			return new ShardedCriteriaImpl(this, entityName, s => s.CreateCriteria(entityName, alias));
		}

		public IQueryOver<T, T> QueryOver<T>() where T : class
		{
			return new ShardedQueryOver<T>((ShardedCriteriaImpl)CreateCriteria<T>());
		}

		public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
		{
			string aliasPath = ExpressionProcessor.FindMemberExpression(alias.Body);
			return new ShardedQueryOver<T>((ShardedCriteriaImpl)CreateCriteria<T>(aliasPath));
		}

		public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class
		{
			return new ShardedQueryOver<T>((ShardedCriteriaImpl)CreateCriteria(entityName));
		}

		public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias) where T : class
		{
			string aliasPath = ExpressionProcessor.FindMemberExpression(alias.Body);
			return new ShardedQueryOver<T>((ShardedCriteriaImpl)CreateCriteria(entityName, aliasPath));
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

		public Task<IQuery> CreateFilterAsync(object collection, string queryString, CancellationToken cancellationToken = new CancellationToken())
		{
			var shard = GetShardForCollection(collection, shards);

			// If collection is not associated with any of our shards, we just delegate to
			// a random shard. We'll end up failing, but we'll fail with the error that users 
			// typically get.
			var session = shard == null
				? AnySession
				: shard.EstablishSession();
			return session.CreateFilterAsync(collection, queryString, cancellationToken);
		}

		private IShard GetShardForCollection(object collection, IEnumerable<IShard> shardsToConsider)
		{
			foreach (IShard shard in shardsToConsider)
			{
				if (this.establishedSessionsByShard.TryGetValue(shard, out var session))
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
		/// of all the criteria.
		/// </summary>
		/// <returns></returns>
		public IMultiCriteria CreateMultiCriteria()
		{
			return new ShardedMultiCriteriaImpl(this);
		}

		public IQueryable<T> Query<T>()
		{
			return new NhQueryable<T>(GetSessionImplementation());
		}

		public IQueryable<T> Query<T>(string entityName)
		{
			return new NhQueryable<T>(GetSessionImplementation(), entityName);
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
			var key = new ShardedEntityKey(GuessEntityName(clazz), id);
			return Get(key, null).Value;
		}

		public async Task<object> GetAsync(System.Type clazz, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(GuessEntityName(clazz), id);
			return (await GetAsync(key, null, cancellationToken)).Value;
		}

		public object Get(string entityName, object id)
		{
			var key = new ShardedEntityKey(entityName, id);
			return Get(key, null).Value;
		}

		public async Task<object> GetAsync(string entityName, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(entityName, id);
			return (await GetAsync(key, null, cancellationToken)).Value;
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
			var key = new ShardedEntityKey(GuessEntityName(clazz), id);
			return Get(key, lockMode).Value;
		}

		public async Task<object> GetAsync(System.Type clazz, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(GuessEntityName(clazz), id);
			return (await GetAsync(key, lockMode, cancellationToken)).Value;
		}

		/// <summary>
		/// Strongly-typed version of <see cref="Get{T}(object)" />
		/// </summary>
		public T Get<T>(object id)
		{
			var key = new ShardedEntityKey(GuessEntityName(typeof(T)), id);
			return (T)Get(key, null).Value;
		}

		public async Task<T> GetAsync<T>(object id, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(GuessEntityName(typeof(T)), id);
			return (T)(await GetAsync(key, null, cancellationToken)).Value;
		}

		/// <summary>
		/// Strongly-typed version of <see cref="Get{T}(object,LockMode)" />
		/// </summary>
		public T Get<T>(object id, LockMode lockMode)
		{
			var key = new ShardedEntityKey(GuessEntityName(typeof(T)), id);
			return (T)Get(key, lockMode).Value;
		}

		public async Task<T> GetAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			var key = new ShardedEntityKey(GuessEntityName(typeof(T)), id);
			return (T)(await GetAsync(key, lockMode, cancellationToken)).Value;
		}

		private IUniqueResult<object> Get(ShardedEntityKey key, LockMode mode)
		{
			// we're not letting people customize shard selection by lockMode
			var shardOperation = new GetShardOperation(key, mode);
			var exitStrategy = new UniqueResultExitStrategy<object>(null);
			this.shardedSessionFactory.ShardStrategy.ShardAccessStrategy.Apply(ResolveToShards(key), shardOperation, exitStrategy);
			return exitStrategy;
		}

		private async Task<IUniqueResult<object>> GetAsync(ShardedEntityKey key, LockMode mode, CancellationToken cancellationToken)
		{
			// we're not letting people customize shard selection by lockMode
			var shardOperation = new GetShardOperation(key, mode);
			var exitStrategy = new UniqueResultExitStrategy<object>(null);
			await this.shardedSessionFactory.ShardStrategy.ShardAccessStrategy.ApplyAsync(ResolveToShards(key), shardOperation, exitStrategy, cancellationToken);
			return exitStrategy;
		}

		private bool TryGet(ShardedEntityKey key, LockMode lockMode, out IUniqueResult<object> result)
		{
			if (key.IsNull)
			{
				result = null;
				return false;
			}

			result = Get(key, lockMode);
			return result.Value != null;
		}

		private Task<IUniqueResult<object>> TryGetAsync(ShardedEntityKey key, LockMode lockMode, CancellationToken cancellationToken)
		{
			return key.IsNull
				? Task.FromResult(default(IUniqueResult<object>))
				: GetAsync(key, lockMode, cancellationToken);
		}

		private class GetShardOperation : IShardOperation<object>, IAsyncShardOperation<object>
		{
			private readonly ShardedEntityKey key;
			private readonly LockMode lockMode;

			public GetShardOperation(ShardedEntityKey key, LockMode lockMode)
			{
				this.key = key;
				this.lockMode = lockMode;
			}

			public Func<object> Prepare(IShard shard)
			{
				// TODO: NHibernate seems to miss an ISession.Get(string entityName, object id, LockMode lockMode) overload.
				var session = shard.EstablishSession();
				if (this.lockMode == null)
				{
					return () => session.Get(this.key.EntityName, this.key.Id);
				}

				return () =>
				{
					try
					{
						return session.Load(this.key.EntityName, this.key.Id, this.lockMode);
					}
					catch (ObjectNotFoundException)
					{
						return null;
					}
				};
			}

			public Func<CancellationToken, Task<object>> PrepareAsync(IShard shard)
			{
				// TODO: NHibernate seems to miss an ISession.Get(string entityName, object id, LockMode lockMode) overload.
				var session = shard.EstablishSession();
				if (this.lockMode == null)
				{
					return ct => session.GetAsync(this.key.EntityName, this.key.Id, ct);
				}

				return ct =>
				{
					try
					{
						return session.LoadAsync(this.key.EntityName, this.key.Id, this.lockMode, ct);
					}
					catch (ObjectNotFoundException)
					{
						return null;
					}
				};
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
			return GetSessionForAttachedObject(obj).GetEntityName(obj);
		}

		public Task<string> GetEntityNameAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return GetSessionForAttachedObject(obj).GetEntityNameAsync(obj, cancellationToken);
		}

		/**
		  * Helper method we can use when we need to find the Shard with which a
		  * specified object is associated. If the object isn't associated with a
		  * Session we just return a random Session with the expectation that this
		  * will cause an error.
		  */
		private ISession GetSessionForAttachedObject(object obj)
		{
			return TryGetShardForAttachedEntity(obj, out var shard)
				? shard.EstablishSession()
				: AnySession;
		}

		#region Filter

		/// <summary>
		/// Enable the named filter for this current session.
		/// </summary>
		/// <param name="filterName">The name of the filter to be enabled.</param>
		/// <returns>The Filter instance representing the enabled filter.</returns>
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
		/// <returns>The Filter instance representing the enabled filter.</returns>
		public IFilter GetEnabledFilter(string filterName)
		{
			return (this.enabledFilters != null && this.enabledFilters.TryGetValue(filterName, out var result))
				? result
				: null;
		}

		/// <summary>
		/// Disable the named filter for the current session.
		/// </summary>
		/// <param name="filterName">The name of the filter to be disabled.</param>
		public void DisableFilter(string filterName)
		{
			if (this.enabledFilters != null && this.enabledFilters.TryGetValue(filterName, out var filter))
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
			ApplyActionToShards(s => s.SetBatchSize(batchSize));
			return this;
		}

		public ISession GetSession(EntityMode entityMode)
		{
			return this;
		}

		/// <summary>
		/// Gets the session implementation.
		/// </summary>
		/// <remarks>
		/// This method is provided in order to get the <b>NHibernate</b> implementation of the session from wrapper implementations.
		/// Implementors of the <seealso cref="ISession"/> interface should return the NHibernate implementation of this method.
		/// </remarks>
		/// <returns>
		/// An NHibernate implementation of the <seealso cref="ISessionImplementor"/> interface 
		/// </returns>
		public ISessionImplementor GetSessionImplementation()
		{
			return this.sessionImpl ?? (this.sessionImpl = new DelegatingSessionImpl(this));
		}

		/// <summary> Get the statistics for this session.</summary>
		public ISessionStatistics Statistics
		{
			get
			{
				if (this.statistics == null)
				{
					this.statistics = new ShardedSessionStatistics();
					ApplyActionToShards(s => statistics.CollectFor(s));
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
					Log.Warn(e, "Caught exception trying to close.");
				}
			}
		}

		#endregion

		#region Private methods

		private string GuessEntityName(object entity)
		{
			if (entity is INHibernateProxy proxy)
			{
				var initializer = proxy.HibernateLazyInitializer;
				entity = initializer.GetImplementation();
			}

			var entityName = interceptor?.GetEntityName(entity);
			return entityName ?? GuessEntityName(entity.GetType());
		}

		private string GuessEntityName(System.Type persistentClass)
		{
			return this.shardedSessionFactory.TryGetGuessEntityName(persistentClass)
				   ?? persistentClass.AssemblyQualifiedName;
		}

		private bool TryResolveToSingleShard(ShardedEntityKey key, out IShard result)
		{
			if (TryResolveToSingleShardId(key, out var shardId))
			{
				result = this.shardsById[shardId];
				return true;
			}

			result = null;
			return false;
		}

		private IEnumerable<IShard> ResolveToShards(ShardedEntityKey key)
		{
			IShard firstResolvedShard = null;
			HashSet<IShard> resolvedShards = null;

			foreach (var shardId in ResolveToShardIds(key))
			{
				var resolvedShard = this.shardsById[shardId];
				if (firstResolvedShard == null)
				{
					firstResolvedShard = resolvedShard;
				}
				else if (resolvedShards != null)
				{
					resolvedShards.Add(resolvedShard);
				}
				else if (resolvedShard != firstResolvedShard)
				{
					resolvedShards = new HashSet<IShard> { firstResolvedShard, resolvedShard };
				}
			}

			if (resolvedShards != null) return resolvedShards;
			if (firstResolvedShard != null) return new SingletonEnumerable<IShard>(firstResolvedShard);
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
			if (!key.IsNull && this.shardedSessionFactory.TryExtractShardIdFromKey(key, out var singleShardId))
			{
				return new SingletonEnumerable<ShardId>(singleShardId);
			}

			return this.shardedSessionFactory.ShardStrategy.ShardResolutionStrategy.ResolveShardIds(key);
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
					ThrowIfConflictingShardId(entityName, expectedShardId, associatedShardId);
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
				return base.OnFlushDirty(entity, id, currentState, previousState, propertyNames, types);
			}

			public override void OnCollectionUpdate(object collection, object key)
			{
				this.detector.OnCollectionUpdate(collection, key);
				base.OnCollectionUpdate(collection, key);
			}
		}

		private class DelegatingSessionImpl : ISessionImplementor
		{
			private readonly Guid sessionId = Guid.NewGuid();
			private readonly ShardedSessionImpl shardedSession;
			private readonly ISessionImplementor anySessionImplementor;


			public DelegatingSessionImpl(ShardedSessionImpl shardedSession)
			{
				this.shardedSession = shardedSession;
				this.anySessionImplementor = shardedSession.AnySession.GetSessionImplementation();
			}

			public long Timestamp
			{
				get {  throw new NotImplementedException(); }
			}

			public ISessionFactoryImplementor Factory
			{
				get { return this.anySessionImplementor.Factory; }
			}

			public IBatcher Batcher
			{
				get { throw new NotImplementedException(); }
			}

			public IDictionary<string, IFilter> EnabledFilters
			{
				get { return this.anySessionImplementor.EnabledFilters; }
			}

			public IInterceptor Interceptor
			{
				get { return this.anySessionImplementor.Interceptor; }
			}

			public EventListeners Listeners
			{
				get { return this.anySessionImplementor.Listeners; }
			}

			public ConnectionManager ConnectionManager
			{
				get { throw new NotImplementedException(); }
			}

			public bool IsEventSource
			{
				get { return this.anySessionImplementor.IsEventSource; }
			}

			public IPersistenceContext PersistenceContext
			{
				get { throw new NotImplementedException(); }
			}

			public CacheMode CacheMode
			{
				get { return this.shardedSession.CacheMode; }
				set { this.shardedSession.CacheMode = value; }
			}

			public bool IsOpen
			{
				get { return this.anySessionImplementor.IsOpen; }
			}

			public bool IsConnected
			{
				get { return this.anySessionImplementor.IsConnected; }
			}

			public FlushMode FlushMode
			{
				get { return this.anySessionImplementor.FlushMode; }
				set { this.shardedSession.FlushMode = value; }
			}

			public string FetchProfile
			{
				get { return this.anySessionImplementor.FetchProfile; }
				set { throw new NotImplementedException(); }
			}

			public DbConnection Connection
			{
				get { return this.shardedSession.Connection; }
			}

			public bool IsClosed
			{
				get { return this.shardedSession.closed; }
			}

			public bool TransactionInProgress
			{
				get { return this.anySessionImplementor.TransactionInProgress; }
			}

			public FutureCriteriaBatch FutureCriteriaBatch
			{
				get { throw new NotImplementedException(); }
			}

			public FutureQueryBatch FutureQueryBatch
			{
				get { throw new NotImplementedException(); }
			}

			public Guid SessionId
			{
				get { return this.sessionId; }
			}

			public ITransactionContext TransactionContext
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public void Initialize()
			{}

			public void InitializeCollection(IPersistentCollection collection, bool writing)
			{
				throw new NotImplementedException();
			}

			public Task InitializeCollectionAsync(IPersistentCollection collection, bool writing, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public object InternalLoad(string entityName, object id, bool eager, bool isNullable)
			{
				throw new NotImplementedException();
			}

			public Task<object> InternalLoadAsync(string entityName, object id, bool eager, bool isNullable, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public object ImmediateLoad(string entityName, object id)
			{
				throw new NotImplementedException();
			}

			public Task<object> ImmediateLoadAsync(string entityName, object id, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IList List(string query, QueryParameters parameters)
			{
				throw new NotImplementedException();
			}

			public IList List(IQueryExpression queryExpression, QueryParameters parameters)
			{
				throw new NotImplementedException();
			}

			public IQuery CreateQuery(IQueryExpression queryExpression)
			{
				var queryPlan = this.anySessionImplementor.Factory.QueryPlanCache.GetHQLQueryPlan(
					queryExpression, false, this.anySessionImplementor.EnabledFilters);
				return new ShardedQueryImpl(this.shardedSession, queryPlan);
			}

			public void List(string query, QueryParameters parameters, IList results)
			{
				throw new NotImplementedException();
			}

			public void List(IQueryExpression queryExpression, QueryParameters queryParameters, IList results)
			{
				throw new NotImplementedException();
			}

			public Task ListAsync(IQueryExpression queryExpression, QueryParameters queryParameters, IList results, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public Task<IList> ListAsync(IQueryExpression queryExpression, QueryParameters parameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public Task<IList<T>> ListAsync<T>(IQueryExpression queryExpression, QueryParameters queryParameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IList<T> List<T>(string query, QueryParameters queryParameters)
			{
				throw new NotImplementedException();
			}

			public IList<T> List<T>(IQueryExpression queryExpression, QueryParameters queryParameters)
			{
				throw new NotImplementedException();
			}

			public IList<T> List<T>(CriteriaImpl criteria)
			{
				throw new NotImplementedException();
			}

			public Task<IList<T>> ListAsync<T>(CriteriaImpl criteria, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public void List(CriteriaImpl criteria, IList results)
			{
				throw new NotImplementedException();
			}

			public Task ListAsync(CriteriaImpl criteria, IList results, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IList List(CriteriaImpl criteria)
			{
				throw new NotImplementedException();
			}

			public Task<IList> ListAsync(CriteriaImpl criteria, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IEnumerable Enumerable(string query, QueryParameters parameters)
			{
				throw new NotImplementedException();
			}

			public IEnumerable Enumerable(IQueryExpression query, QueryParameters parameters)
			{
				throw new NotImplementedException();
			}

			public Task<IEnumerable> EnumerableAsync(IQueryExpression query, QueryParameters parameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IEnumerable<T> Enumerable<T>(string query, QueryParameters queryParameters)
			{
				throw new NotImplementedException();
			}

			public IEnumerable<T> Enumerable<T>(IQueryExpression query, QueryParameters queryParameters)
			{
				throw new NotImplementedException();
			}

			public Task<IEnumerable<T>> EnumerableAsync<T>(IQueryExpression query, QueryParameters queryParameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IList ListFilter(object collection, string filter, QueryParameters parameters)
			{
				throw new NotImplementedException();
			}

			public Task<IList> ListFilterAsync(object collection, string filter, QueryParameters parameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IList ListFilter(object collection, IQueryExpression queryExpression, QueryParameters parameters)
			{
				throw new NotImplementedException();
			}

			public Task<IList> ListFilterAsync(object collection, IQueryExpression queryExpression, QueryParameters parameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IList<T> ListFilter<T>(object collection, string filter, QueryParameters parameters)
			{
				throw new NotImplementedException();
			}

			public Task<IList<T>> ListFilterAsync<T>(object collection, string filter, QueryParameters parameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IEnumerable EnumerableFilter(object collection, string filter, QueryParameters parameters)
			{
				throw new NotImplementedException();
			}

			public Task<IEnumerable> EnumerableFilterAsync(object collection, string filter, QueryParameters parameters,
				CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IEnumerable<T> EnumerableFilter<T>(object collection, string filter, QueryParameters parameters)
			{
				throw new NotImplementedException();
			}

			public Task<IEnumerable<T>> EnumerableFilterAsync<T>(object collection, string filter, QueryParameters parameters,
				CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IEntityPersister GetEntityPersister(string entityName, object obj)
			{
				throw new NotImplementedException();
			}

			public void AfterTransactionBegin(ITransaction tx)
			{
				throw new NotImplementedException();
			}

			public void BeforeTransactionCompletion(ITransaction tx)
			{
				throw new NotImplementedException();
			}

			public Task BeforeTransactionCompletionAsync(ITransaction tx, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public void AfterTransactionCompletion(bool successful, ITransaction tx)
			{
				throw new NotImplementedException();
			}

			public Task AfterTransactionCompletionAsync(bool successful, ITransaction tx, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public void FlushBeforeTransactionCompletion()
			{
				throw new NotImplementedException();
			}

			public Task FlushBeforeTransactionCompletionAsync(CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public object GetContextEntityIdentifier(object obj)
			{
				throw new NotImplementedException();
			}

			public object Instantiate(string entityName, object id)
			{
				throw new NotImplementedException();
			}

			public IList List(NativeSQLQuerySpecification spec, QueryParameters queryParameters)
			{
				throw new NotImplementedException();
			}

			public Task<IList> ListAsync(NativeSQLQuerySpecification spec, QueryParameters queryParameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public void List(NativeSQLQuerySpecification spec, QueryParameters queryParameters, IList results)
			{
				throw new NotImplementedException();
			}

			public Task ListAsync(NativeSQLQuerySpecification spec, QueryParameters queryParameters, IList results, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IList<T> List<T>(NativeSQLQuerySpecification spec, QueryParameters queryParameters)
			{
				throw new NotImplementedException();
			}

			public Task<IList<T>> ListAsync<T>(NativeSQLQuerySpecification spec, QueryParameters queryParameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public void ListCustomQuery(ICustomQuery customQuery, QueryParameters queryParameters, IList results)
			{
				throw new NotImplementedException();
			}

			public Task ListCustomQueryAsync(ICustomQuery customQuery, QueryParameters queryParameters, IList results, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public IList<T> ListCustomQuery<T>(ICustomQuery customQuery, QueryParameters queryParameters)
			{
				throw new NotImplementedException();
			}

			public Task<IList<T>> ListCustomQueryAsync<T>(ICustomQuery customQuery, QueryParameters queryParameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public object GetFilterParameterValue(string filterParameterName)
			{
				throw new NotImplementedException();
			}

			public IType GetFilterParameterType(string filterParameterName)
			{
				throw new NotImplementedException();
			}

			public IQuery GetNamedSQLQuery(string name)
			{
				return this.anySessionImplementor.GetNamedSQLQuery(name);
			}

			public IQueryTranslator[] GetQueries(string query, bool scalar)
			{
				throw new NotImplementedException();
			}

			public IQueryTranslator[] GetQueries(IQueryExpression query, bool scalar)
			{
				throw new NotImplementedException();
			}

			public Task<IQueryTranslator[]> GetQueriesAsync(IQueryExpression query, bool scalar, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public object GetEntityUsingInterceptor(EntityKey key)
			{
				throw new NotImplementedException();
			}

			public Task<object> GetEntityUsingInterceptorAsync(EntityKey key, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public string BestGuessEntityName(object entity)
			{
				throw new NotImplementedException();
			}

			public string GuessEntityName(object entity)
			{
				return this.shardedSession.GuessEntityName(entity);
			}

			public IQuery GetNamedQuery(string queryName)
			{
				return ShardedQueryImpl.GetNamedQuery(this.shardedSession, queryName);
			}

			public void Flush()
			{
				this.shardedSession.Flush();
			}

			public Task FlushAsync(CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public int ExecuteNativeUpdate(NativeSQLQuerySpecification specification, QueryParameters queryParameters)
			{
				throw new NotImplementedException();
			}

			public Task<int> ExecuteNativeUpdateAsync(NativeSQLQuerySpecification specification, QueryParameters queryParameters,
				CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public int ExecuteUpdate(string query, QueryParameters queryParameters)
			{
				throw new NotImplementedException();
			}

			public int ExecuteUpdate(IQueryExpression query, QueryParameters queryParameters)
			{
				throw new NotImplementedException();
			}

			public Task<int> ExecuteUpdateAsync(IQueryExpression query, QueryParameters queryParameters, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public void JoinTransaction()
			{
				throw new NotImplementedException();
			}

			public void CloseSessionFromSystemTransaction()
			{
				throw new NotImplementedException();
			}

			public void CloseSessionFromDistributedTransaction()
			{
				throw new NotImplementedException();
			}

			public IQuery CreateFilter(object collection, IQueryExpression queryExpression)
			{
				throw new NotImplementedException();
			}

			public Task<IQuery> CreateFilterAsync(object collection, IQueryExpression queryExpression, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}

			public EntityKey GenerateEntityKey(object id, IEntityPersister persister)
			{
				throw new NotImplementedException();
			}

			public CacheKey GenerateCacheKey(object id, IType type, string entityOrRoleName)
			{
				throw new NotImplementedException();
			}
		}

		#endregion
	}
}