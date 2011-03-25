using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetShortEvent : IQueryEvent
	{
		private readonly CtorType ctorType;
		private readonly String name;
		private readonly int position;
		private readonly short val;

		private SetShortEvent(CtorType ctorType, int position, short val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetShortEvent(int position, short val) :
			this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetShortEvent(String name, short val) :
			this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetInt16(position, val);
					break;
				case CtorType.NameVal:
					query.SetInt16(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetShortEvent: " + ctorType);
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
