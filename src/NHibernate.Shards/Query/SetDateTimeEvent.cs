using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetDateTimeEvent : IQueryEvent
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

		private SetDateTimeEvent(CtorType ctorType, int position, DateTime val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetDateTimeEvent(int position, DateTime val)
			: this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetDateTimeEvent(String name, DateTime val)
			: this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetDateTime(position, val);
					break;
				case CtorType.NameVal:
					query.SetDateTime(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetDateTimeEvent: " + ctorType);
			}
		}

	}

}
