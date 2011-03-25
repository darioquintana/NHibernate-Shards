using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetIntegerEvent : IQueryEvent
	{
		private readonly CtorType ctorType;
		private readonly String name;
		private readonly int position;
		private readonly int val;

		private SetIntegerEvent(CtorType ctorType, int position, int val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetIntegerEvent(int position, int val) :
			this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetIntegerEvent(String name, int val) :
			this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetInt32(position, val);
					break;
				case CtorType.NameVal:
					query.SetInt32(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetIntegerEvent: " + ctorType);
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
