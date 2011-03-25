using System;
using NHibernate.Shards.Session;
using NHibernate.Type;

namespace NHibernate.Shards.Query
{
	public class SetParameterEvent : IQueryEvent
	{
		private readonly CtorType ctorType;
		private readonly String name;

		private readonly int position;
		private readonly IType type;
		private readonly object val;


		private SetParameterEvent(CtorType ctorType, int position, String name, object val, IType type)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.type = type;
			this.name = name;
		}

		public SetParameterEvent(int position, object val, IType type) :
			this(CtorType.PositionValType, position, null, val, type)
		{
		}

		public SetParameterEvent(String name, object val, IType type) :
			this(CtorType.NameValType, -1, name, val, type)
		{
		}

		public SetParameterEvent(int position, Object val) :
			this(CtorType.PositionVal, position, null, val, null)
		{
		}

		public SetParameterEvent(String name, Object val) :
			this(CtorType.NameVal, -1, name, val, null)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetParameter(position, val);
					break;
				case CtorType.PositionValType:
					query.SetParameter(position, val, type);
					break;
				case CtorType.NameVal:
					query.SetParameter(name, val);
					break;
				case CtorType.NameValType:
					query.SetParameter(name, val, type);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetParameterEvent: " + ctorType);
			}
		}

		#region Nested type: CtorType

		private enum CtorType
		{
			PositionVal,
			PositionValType,
			NameVal,
			NameValType,
		}

		#endregion
	}
}
