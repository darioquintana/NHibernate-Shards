using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetLongEvent : IQueryEvent
	{
		private readonly CtorType ctorType;
		private readonly String name;
		private readonly int position;
		private readonly long val;

		private SetLongEvent(CtorType ctorType, int position, long val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetLongEvent(int position, long val) :
			this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetLongEvent(String name, long val) :
			this(CtorType.NameVal, -1, val, name)
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
						"Unknown ctor type in SetLongEvent: " + ctorType);
			}
		}

		#region Nested type: CtorType

		private enum CtorType
		{
			PositionVal,
			NameVal
		}

		#endregion
	}
}
