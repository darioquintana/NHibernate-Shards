using System;

namespace NHibernate.Shards.Query
{
	public class SetLockModeEvent : IQueryEvent
	{
		private readonly String alias;
		private readonly LockMode lockMode;

		public SetLockModeEvent(String alias, LockMode lockMode)
		{
			this.alias = alias;
			this.lockMode = lockMode;
		}

		public void OnEvent(IQuery query)
		{
			query.SetLockMode(alias, lockMode);
		}
	}
}
