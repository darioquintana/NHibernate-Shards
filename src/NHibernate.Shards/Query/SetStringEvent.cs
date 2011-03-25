using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetStringEvent : IQueryEvent
	{
		private enum CtorType
		{
			PositionVal,
			NameVal
		}

		private readonly CtorType ctorType;
		private readonly int position;
		private readonly String val;
		private readonly String name;

		private SetStringEvent(CtorType ctorType, int position, String val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetStringEvent(int position, String val)
			: this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetStringEvent(String name, String val)
			: this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetString(position, val);
					break;
				case CtorType.NameVal:
					query.SetString(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetStringEvent: " + ctorType);
			}
		}

	}
}
