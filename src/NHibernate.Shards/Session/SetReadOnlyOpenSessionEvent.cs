using System;

namespace NHibernate.Shards.Session
{
	//TODO: Make pass the test at NH on NHibernate.Tests.ReadOnlyTests
	public class SetReadOnlyOpenSessionEvent : IOpenSessionEvent
	{
		private readonly object entity;

		public SetReadOnlyOpenSessionEvent(object entity, bool readOnly)
		{
			this.entity = entity;
			this.readOnly = readOnly;
		}

		private readonly bool readOnly;

		public void OnOpenSession(ISession session)
		{
			throw new NotImplementedException();
		}
	}
}