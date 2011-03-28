using System.Collections.Generic;
using NHibernate.Engine;
using NHibernate.Shards.Id;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Engine
{

    /// <summary>
    /// Internal interface for implementors of ShardedSessionFactory
    /// </summary>
    public interface IShardedSessionFactoryImplementor : IShardedSessionFactory, ISessionFactoryImplementor
    {
        /// <summary>
        /// The session factory to be used for operations that cannot be distributed across multiple shards,
        /// such as the calculation of shard-wide unique sequence numbers and identifiers.
        /// </summary>
        /// <seealso cref="IShardEncodingIdentifierGenerator"/>
        ISessionFactoryImplementor ControlFactory { get; }

        /// <summary>
        /// Enumerates meta data for all shards that are within the scope of this sharded session factory.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IShardMetadata> GetShardMetadata();
    }

    public static class ShardedSessionFactoryImplementorUtil
    {
        /// <summary>
        /// Attempts to extract <see cref="ShardId"/> from the entity identifier. This 
        /// will only work if the shard identifier has been encoded into the entity 
        /// identifier by an <see cref="IShardEncodingIdentifierGenerator"/>.
        /// </summary>
        /// <param name="shardedSessionFactory">The sharded session factory to use to retrieve entity metadata.</param>
        /// <param name="key">The entity key.</param>
        /// <param name="result">Returns the extracted <see cref="ShardId"/> if this operation succeeds.</param>
        /// <returns>Returns <c>true</c> if this operation succeeds or false otherwise.</returns>
        public static bool TryExtractShardIdFromKey(
            this IShardedSessionFactoryImplementor shardedSessionFactory, ShardedEntityKey key, out ShardId result)
        {
            var sessionFactory = shardedSessionFactory.ControlFactory;
            var entityPersister = sessionFactory.GetEntityPersister(key.EntityName);
            var rootEntityName = entityPersister.RootEntityName;

            var idGenerator = sessionFactory.GetIdentifierGenerator(rootEntityName) as IShardEncodingIdentifierGenerator;
            if (idGenerator != null)
            {
                result = idGenerator.ExtractShardId(key.Id);
                return true;
            }

            result = null;
            return false;
        }

        public static string GuessEntityName(
            this IShardedSessionFactoryImplementor shardedSessionFactory, System.Type entityType)
        {
            return shardedSessionFactory.ControlFactory.TryGetGuessEntityName(entityType);
        }
    }
}