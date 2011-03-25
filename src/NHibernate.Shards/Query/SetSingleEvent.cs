using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetSingleEvent : IQueryEvent
	{
		private enum CtorType
		{
			PositionVal,
			NameVal
		}

		private readonly CtorType ctorType;
		private readonly int position;
		private readonly float val;
		private readonly String name;

		private SetSingleEvent(CtorType ctorType, int position, float val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetSingleEvent(int position, float val)
			: this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetSingleEvent(String name, float val)
			: this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetSingle(position, val);
					break;
				case CtorType.NameVal:
					query.SetSingle(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetFloatEvent: " + ctorType);
			}
		}

	}
}
