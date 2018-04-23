namespace NHibernate.Shards.Session
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using NHibernate.Shards.Engine;
    using NHibernate.Shards.Util;

    public abstract class BaseShardedSessionBuilder<TShardedBuilder, TBuilder> : IShardedSessionBuilderImplementor 
        where TShardedBuilder : BaseShardedSessionBuilder<TShardedBuilder, TBuilder>
        where TBuilder : ISessionBuilder<TBuilder>
    {
        #region Instance fields

        private readonly List<Action<TBuilder>> establishActions = new List<Action<TBuilder>>();
        private IInterceptor sessionInterceptor;

        #endregion

        #region Properties

        public IInterceptor SessionInterceptor
        {
            get { return this.sessionInterceptor; }
        }

        #endregion

        #region Public Methods

        public TShardedBuilder Interceptor(IInterceptor interceptor)
        {
            Preconditions.CheckNotNull(interceptor);
            this.sessionInterceptor = interceptor;
            return (TShardedBuilder)this;
        }

        public TShardedBuilder NoInterceptor()
        {
            this.sessionInterceptor = null;
            return (TShardedBuilder)this;
        }

        public TShardedBuilder Connection(DbConnection connection)
        {
            throw new NotSupportedException("Cannot open a sharded session with a user provided connection.");
        }

        public TShardedBuilder ConnectionReleaseMode(ConnectionReleaseMode connectionReleaseMode)
        {
            this.establishActions.Add(b => b.ConnectionReleaseMode(connectionReleaseMode));
            return (TShardedBuilder)this;
        }

        public TShardedBuilder AutoClose(bool autoClose)
        {
            this.establishActions.Add(b => b.AutoClose(autoClose));
            return (TShardedBuilder)this;
        }

        public TShardedBuilder AutoJoinTransaction(bool autoJoinTransaction)
        {
            this.establishActions.Add(b => b.AutoJoinTransaction(autoJoinTransaction));
            return (TShardedBuilder)this;
        }

        public TShardedBuilder FlushMode(FlushMode flushMode)
        {
            this.establishActions.Add(b => b.FlushMode(flushMode));
            return (TShardedBuilder)this;
        }

        public abstract IShardedSession OpenSession();

        public ISession OpenSessionFor(IShard shard, IInterceptor interceptor)
        {
            var result = CreateBuilderFor(shard);
            foreach (var establishAction in this.establishActions)
            {
                establishAction(result);
            }

            if (interceptor != null)
            {
                result.Interceptor(interceptor);
            }
            else
            {
                result.NoInterceptor();
            }

            return result.OpenSession();
        }

        #endregion

        #region Protected methods

        protected abstract TBuilder CreateBuilderFor(IShard shard);

        #endregion
    }
}