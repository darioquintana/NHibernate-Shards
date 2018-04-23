namespace NHibernate.Shards.Session
{
    using System.Data.Common;
    using NHibernate.Shards.Engine;
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

        #region ISharedSessionBuilder Implementation

        /// <inheritdoc />
        ISession ISessionBuilder<ISharedSessionBuilder>.OpenSession()
        {
            return OpenSession();
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
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.Connection(DbConnection connection)
        {
            return Connection(connection);
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.ConnectionReleaseMode(ConnectionReleaseMode connectionReleaseMode)
        {
            return ConnectionReleaseMode(connectionReleaseMode);
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.AutoClose(bool autoClose)
        {
            return AutoClose(autoClose);
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.AutoJoinTransaction(bool autoJoinTransaction)
        {
            return AutoJoinTransaction(autoJoinTransaction);
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISessionBuilder<ISharedSessionBuilder>.FlushMode(FlushMode flushMode)
        {
            return FlushMode(flushMode);
        }

        /// <inheritdoc />

        /// <inheritdoc />
        ISharedSessionBuilder ISharedSessionBuilder.Interceptor()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISharedSessionBuilder.ConnectionReleaseMode()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISharedSessionBuilder.FlushMode()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISharedSessionBuilder.AutoClose()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        ISharedSessionBuilder ISharedSessionBuilder.AutoJoinTransaction()
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}