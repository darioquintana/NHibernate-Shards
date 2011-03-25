using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetGuidEvent : IQueryEvent
	{
		private enum CtorType
		{
			PositionVal,
			NameVal
		}

		private readonly CtorType ctorType;
		private readonly int position;
		private readonly Guid val;
		private readonly String name;

		private SetGuidEvent(CtorType ctorType, int position, Guid val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetGuidEvent(int position, Guid val)
			: this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetGuidEvent(String name, Guid val)
			: this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetGuid(position, val);
					break;
				case CtorType.NameVal:
					query.SetGuid(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetGuidEvent: " + ctorType);
			}
		}

	}
}
