using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetBigIntegerEvent : IQueryEvent
	{
		private enum CtorType
		{
			PositionVal,
			NameVal
		}

		private readonly CtorType ctorType;
		private readonly int position;
		private readonly Int64 val;
		private readonly String name;

		private SetBigIntegerEvent(CtorType ctorType, int position, Int64 val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetBigIntegerEvent(int position, Int64 val)
			: this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetBigIntegerEvent(String name, Int64 val)
			: this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetInt64(position, val);
					break;
				case CtorType.NameVal:
					query.SetInt64(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetEvent: " + ctorType);
			}
		}
	}
}