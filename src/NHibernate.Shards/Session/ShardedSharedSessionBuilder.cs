namespace NHibernate.Shards.Session
{
    using System.Data.Common;
    using NHibernate.Shards.Util;

    public class ShardedSharedSessionBuilder : BaseShardedSessionBuilder<ShardedSharedSessionBuilder, ISharedSessionBuilder>, ISharedSessionBuilder
    {
        private readonly ShardedSessionImpl shardedSession;

        internal ShardedSharedSessionBuilder(ShardedSessionImpl shardedSession)
        {
            Preconditions.CheckNotNull(shardedSession);
            this.shardedSession = shardedSession;
        }

        #region Overrides

        public override IShardedSession OpenSession()
        {
            return new ShardedSessionImpl(this.shardedSession, this);
        }

        protected override ISharedSessionBuilder CreateBuilderFor(IShard shard)
        {
            return this.shardedSession.EstablishFor(shard).SessionWithOptions();
        }

        #endregion

        #region Public methods

        private ShardedSharedSessionBuilder Connection()
        {
            ApplyActionToShards(b => b.Connection());
            return this;
        }

        public ShardedSharedSessionBuilder Interceptor()
        {
            ApplyActionToShards(b => b.Interceptor());
            return this;
        }

        public ShardedSharedSessionBuilder ConnectionReleaseMode()
        {
            ApplyActionToShards(b => b.ConnectionReleaseMode());
            return this;
        }

        public ShardedSharedSessionBuilder AutoClose()
        {
            ApplyActionToShards(b => b.AutoClose());
            return this;
        }

        public ShardedSharedSessionBuilder AutoJoinTransaction()
        {
            ApplyActionToShards(b => b.AutoJoinTransaction());
            return this;
        }

        public ShardedSharedSessionBuilder FlushMode()
        {
            ApplyActionToShards(b => b.FlushMode());
            return this;
        }

        #endregion

        #region ISharedSessionBuilder Implementation

        /// <inheritdoc />
        ISession ISessionBuilder<ISharedSessionBuilder>.OpenSession()
        {
            return OpenSession();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISharedSessionBuilder.Interceptor()
        {
            Interceptor();
            return this;
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.Interceptor(IInterceptor interceptor)
        {
            return Interceptor(interceptor);
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.NoInterceptor()
        {
            return NoInterceptor();
        }

        ISharedSessionBuilder ISharedSessionBuilder.Connection()
        {
            return Connection();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.Connection(DbConnection connection)
        {
            return Connection(connection);
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISharedSessionBuilder.ConnectionReleaseMode()
        {
            return ConnectionReleaseMode();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.ConnectionReleaseMode(ConnectionReleaseMode connectionReleaseMode)
        {
            return ConnectionReleaseMode(connectionReleaseMode);
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISharedSessionBuilder.AutoClose()
        {
            return AutoClose();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.AutoClose(bool autoClose)
        {
            return AutoClose(autoClose);
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISharedSessionBuilder.AutoJoinTransaction()
        {
            return AutoJoinTransaction();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.AutoJoinTransaction(bool autoJoinTransaction)
        {
            return AutoJoinTransaction(autoJoinTransaction);
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISharedSessionBuilder.FlushMode()
        {
            return FlushMode();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.FlushMode(FlushMode flushMode)
        {
            return FlushMode(flushMode);
        }

        #endregion
    }
}