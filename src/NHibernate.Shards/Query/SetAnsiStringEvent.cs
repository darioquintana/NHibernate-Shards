using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetAnsiStringEvent : IQueryEvent
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

		private SetAnsiStringEvent(CtorType ctorType, int position, String val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetAnsiStringEvent(int position, String val)
			: this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetAnsiStringEvent(String name, String val)
			: this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetAnsiString(position, val);
					break;
				case CtorType.NameVal:
					query.SetAnsiString(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetAnsiStringEvent: " + ctorType);
			}
		}

	}
}
