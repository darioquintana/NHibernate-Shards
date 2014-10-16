using NHibernate.Shards.Engine;
using NHibernate.Shards.Util;

namespace NHibernate.Shards
{
	public class ShardImpl : BaseHasShardIdList, IShard
	{
		private readonly IShardedSessionImplementor shardedSession;

		// the SessionFactory that owns this Session
		private readonly ISessionFactory sessionFactory;

		private ISession session;

		public ShardImpl(IShardedSessionImplementor shardedSession, IShardMetadata shardMetadata)
			: base(shardMetadata.ShardIds)
		{
			Preconditions.CheckNotNull(shardedSession);
			Preconditions.CheckNotNull(shardMetadata);
			this.shardedSession = shardedSession;
			this.sessionFactory = shardMetadata.SessionFactory;
		}

		/// <summary>
		/// SessionFactoryImplementor that owns the Session associated with this Shard
		/// </summary>
		public ISessionFactory SessionFactory
		{
			get { return this.sessionFactory; }
		}

		public ISession Session
		{
			get { return this.session; }
		}

		public bool Contains(object entity)
		{
			return this.session != null
				&& this.session.Contains(entity);
		}

		public ISession EstablishSession()
		{
			if (this.session == null)
			{
				this.session = this.shardedSession.EstablishFor(this);
			}
			return this.session;
		}
	}
}