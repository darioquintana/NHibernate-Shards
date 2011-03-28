using System;
using System.Collections.Generic;
using NHibernate.Shards.Session;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Engine
{

    /// <summary>
    ///  Defines the internal contract between the ShardedSession and other
    /// parts of Hibernate Shards.
    /// <seealso cref="IShardedSession"/> the interface to the application.
    /// <seealso cref="ShardedSessionImpl"/> the actual implementation
    /// </summary>
    public interface IShardedSessionImplementor
    {
        /// <summary>
        /// Returns an arbitrary shard within the scope of this session.
        /// </summary>
        IShard AnyShard { get; }

        /// <summary>
        /// All shards that are within the scope of this session.
        /// </summary>
        IEnumerable<IShard> Shards { get; }

        /// <summary>
        /// Notification that transaction has begun. The session factory should enlist any
        /// established sessions into the transaction during this call.
        /// </summary>
        /// <param name="tx">The sharded transaction that has begun.</param>
        void AfterTransactionBegin(IShardedTransaction tx);

        /// <summary>
        /// Notification of transaction completion.
        /// </summary>
        /// <param name="transaction">The sharded transaction that has completed.</param>
        /// <param name="success">Indicates whether transaction completed successfully.</param>
        void AfterTransactionCompletion(IShardedTransaction transaction, bool? success);

        /// <summary>
        /// Registers an action to be performed once on each shard-local session 
        /// that has been or will be opened within the scope of this sharded
        /// session.
        /// </summary>
        /// <param name="action">The action to be performed once on an opened
        /// shard-local session.</param>
        /// <remarks>
        /// The <see cref="action"/> is performed immediately on all shard-local 
        /// sessions that have already been established. It is also scheduled for
        /// execution when any new shard-local sessions are established within the 
        /// scope of this sharded session.
        /// </remarks>
        void AddEstablishAction(Action<ISession> action);

        /// <summary>
        /// Establishes a shard-local session for a given shard.
        /// </summary>
        /// <param name="shard">The shard for which a session is to be established.</param>
        /// <returns>An open session for the <paramref name="shard"/>.</returns>
        ISession EstablishFor(IShard shard);

        /// <summary>
        /// Performs the specified operation on the shards that are within the scope of
        /// this sharded session and aggregates the results from each shard into a single 
        /// result.
        /// </summary>
        /// <typeparam name="T">Return value type.</typeparam>
        /// <param name="operation">The operation to be performed on each shard.</param>
        /// <param name="exitStrategy">Strategy for collection and aggregation of 
        /// operation results from the shards.</param>
        /// <returns>The aggregated operation result.</returns>
        T Execute<T>(IShardOperation<T> operation, IExitStrategy<T> exitStrategy);
    }
}