using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetTimestampEvent : IQueryEvent
	{
		private enum CtorType
		{
			PositionVal,
			NameVal
		}

		private readonly CtorType ctorType;
		private readonly int position;
		private readonly DateTime val;
		private readonly String name;

		private SetTimestampEvent(CtorType ctorType, int position, DateTime val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetTimestampEvent(int position, DateTime val)
			: this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetTimestampEvent(String name, DateTime val)
			: this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetTimestamp(position, val);
					break;
				case CtorType.NameVal:
					query.SetTimestamp(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetTimestampEvent: " + ctorType);
			}
		}

	}
}
