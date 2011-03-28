using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Criteria
{
    /// <summary>
    /// Interface for a shard-aware <see cref="ICriteria"/> implementation.
    /// <seealso cref="ICriteria"/> 
    /// </summary>
    public interface IShardedCriteria : ICriteria
    {
        /// <summary>
        /// Builds an exit strategy for <see cref="ICriteria.List{T}()"/> operation.
        /// </summary>
        /// <returns>An exit strategy for <see cref="ICriteria.List{T}()"/> operation</returns>
        IListExitStrategy<T> BuildListExitStrategy<T>();

        /// <summary>
        /// Returns an <see cref="ICriteria"/> instance that is associated with the
        /// established session of a given shard.
        /// </summary>
        /// <param name="shard">The shard for which an <see cref="ICriteria"/> is to be established.</param>
        /// <returns>An <see cref="ICriteria"/> instance that is associated with the established session 
        /// of <paramref name="shard"/>.
        /// </returns>
        ICriteria EstablishFor(IShard shard);
    }
}