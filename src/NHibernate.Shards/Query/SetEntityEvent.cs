using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetEntityEvent : IQueryEvent
	{
		private readonly CtorType ctorType;
		private readonly String name;
		private readonly int position;
		private readonly Object val;

		private SetEntityEvent(CtorType ctorType, int position, Object val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetEntityEvent(int position, Object val)
			:
			this(CtorType.PositionVal, position, val, null)
		{
		}


		public SetEntityEvent(String name, Object val)
			:
			this(CtorType.NameVal, -1, val, name)
		{
		}

		#region IQueryEvent Members

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetEntity(position, val);
					break;
				case CtorType.NameVal:
					query.SetEntity(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetEntityEvent: " + ctorType);
			}
		}

		#endregion

		#region Nested type: CtorType

		private enum CtorType
		{
			PositionVal,
			NameVal
		}

		#endregion
	}
}
