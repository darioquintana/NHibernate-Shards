using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetTimeEvent : IQueryEvent
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

		private SetTimeEvent(CtorType ctorType, int position, DateTime val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetTimeEvent(int position, DateTime val)
			: this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetTimeEvent(String name, DateTime val)
			: this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetTime(position, val);
					break;
				case CtorType.NameVal:
					query.SetTime(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetTimeEvent: " + ctorType);
			}
		}

	}
}
