namespace NHibernate.Shards.Session
{
    using System.Data.Common;
    using NHibernate.Shards.Util;

    public class ShardedSessionBuilder : BaseShardedSessionBuilder<ShardedSessionBuilder, ISessionBuilder>, ISessionBuilder
    {
        private readonly ShardedSessionFactoryImpl shardedSessionFactory;

        internal ShardedSessionBuilder(ShardedSessionFactoryImpl shardedSessionFactory)
        {
            Preconditions.CheckNotNull(shardedSessionFactory);
            this.shardedSessionFactory = shardedSessionFactory;
        }

        #region Overrides

        public override IShardedSession OpenSession()
        {
            return new ShardedSessionImpl(this.shardedSessionFactory, this);
        }

        protected override ISessionBuilder CreateBuilderFor(IShard shard)
        {
            return shard.SessionFactory.WithOptions();
        }

        #endregion

        #region ISessionBuilder implementation

        /// <inheritdoc />
        ISessionBuilder ISessionBuilder<ISessionBuilder>.Interceptor(IInterceptor interceptor)
        {
            return Interceptor(interceptor);
        }

        /// <inheritdoc />
        ISessionBuilder ISessionBuilder<ISessionBuilder>.NoInterceptor()
        {
            return NoInterceptor();
        }

        /// <inheritdoc />
        ISessionBuilder ISessionBuilder<ISessionBuilder>.Connection(DbConnection connection)
        {
            return Connection(connection);
        }

        /// <inheritdoc />
        ISessionBuilder ISessionBuilder<ISessionBuilder>.ConnectionReleaseMode(ConnectionReleaseMode connectionReleaseMode)
        {
            return ConnectionReleaseMode(connectionReleaseMode);
        }

        /// <inheritdoc />
        ISessionBuilder ISessionBuilder<ISessionBuilder>.AutoClose(bool autoClose)
        {
            return AutoClose(autoClose);
        }

        /// <inheritdoc />
        ISessionBuilder ISessionBuilder<ISessionBuilder>.AutoJoinTransaction(bool autoJoinTransaction)
        {
            return AutoJoinTransaction(autoJoinTransaction);
        }

        /// <inheritdoc />
        ISessionBuilder ISessionBuilder<ISessionBuilder>.FlushMode(FlushMode flushMode)
        {
            return FlushMode(flushMode);
        }

        /// <inheritdoc />
        ISession ISessionBuilder<ISessionBuilder>.OpenSession()
        {
            return OpenSession();
        }

        #endregion
    }
}