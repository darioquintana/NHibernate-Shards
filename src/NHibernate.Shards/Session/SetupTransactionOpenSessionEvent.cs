namespace NHibernate.Shards.Session
{
	/// <summary>
	/// OpenSessionEvent which adds newly opened session to the specified
	///	ShardedTransaction.
	/// </summary>
	public class SetupTransactionOpenSessionEvent : IOpenSessionEvent
	{
		private readonly IShardedTransaction shardedTransaction;

		public SetupTransactionOpenSessionEvent(IShardedTransaction shardedTtransaction)
		{
			shardedTransaction = shardedTtransaction;
		}

		public void OnOpenSession(ISession session)
		{
			shardedTransaction.SetupTransaction(session);
		}
	}
}