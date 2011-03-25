using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetDecimalEvent : IQueryEvent
	{
		private readonly CtorType ctorType;
		private readonly String name;
		private readonly int position;
		private readonly decimal val;

		private SetDecimalEvent(CtorType ctorType, int position, decimal val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetDecimalEvent(int position, decimal val)
			:
			this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetDecimalEvent(String name, decimal val)
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
					query.SetDecimal(position, val);
					break;
				case CtorType.NameVal:
					query.SetDecimal(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetDecimalEvent: " + ctorType);
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
