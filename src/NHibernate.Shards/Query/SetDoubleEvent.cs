using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetDoubleEvent : IQueryEvent
	{
		private readonly CtorType ctorType;
		private readonly String name;
		private readonly int position;
		private readonly double val;

		private SetDoubleEvent(CtorType ctorType, int position, double val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetDoubleEvent(int position, double val)
			:
			this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetDoubleEvent(String name, double val)
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
					query.SetDouble(position, val);
					break;
				case CtorType.NameVal:
					query.SetDouble(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetDoubleEvent: " + ctorType);
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
