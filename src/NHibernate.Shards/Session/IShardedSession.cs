namespace NHibernate.Shards.Session
{
	using System.Collections.Generic;
	using NHibernate.Multi;
	using NHibernate.Shards.Multi;

	/// <summary>
	/// The main runtime inteface between .Net application and NHibernate Shards.
	/// ShardedSession represents a logical transaction that might be spanning
	/// multiple shards. It follows the contract set by ISession API, and adds some
	/// shard-related methods. 
	/// </summary>
	public interface IShardedSession : ISession
	{
		/// <summary>
		/// Immutable collection of <see cref="ShardId"/>s for shards that are accessible via this session.
		/// </summary>
		ICollection<ShardId> ShardIds { get; }

		/// <summary>
		/// Gets the non-sharded session with which the objects is associated.
		/// </summary>
		/// <param name="obj">the object for which we want the Session</param>
		/// <returns>
		///	The Session with which this object is associated, or null if the
		/// object is not associated with a session belonging to this ShardedSession
		/// </returns>
		ISession GetSessionForObject(object obj);

		/// <summary>
		/// Gets the non-sharded session for a given ShardId.
		/// </summary>
		/// <param name="shardId">A shard identifier</param>
		/// <returns>
		///	The session for the given <paramref name="shardId"/>.
		/// </returns>
		ISession GetSessionForShardId(ShardId shardId);

		/// <summary>
		///  Gets the ShardId of the shard with which the objects is associated.
		/// </summary>
		/// <param name="obj">the object for which we want the Session</param>
		/// <returns>
		/// the ShardId of the Shard with which this object is associated, or
		/// null if the object is not associated with a shard belonging to this
		/// ShardedSession
		/// </returns>
		ShardId GetShardIdForObject(object obj);

		/// <summary>
		/// Place the session into a state where every create operation takes place
		/// on the same shard.  Once the shard is locked on a session it cannot
		/// be unlocked.
		/// </summary>
		void LockShard();

		/// <summary>
		/// Creates shared query batch.
		/// </summary>
		/// <returns>The newly created query batch.</returns>
		IShardedQueryBatch CreateQueryBatch();
	}
}