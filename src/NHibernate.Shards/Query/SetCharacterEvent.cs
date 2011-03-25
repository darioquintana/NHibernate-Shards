using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetCharacterEvent : IQueryEvent
	{
		private enum CtorType
		{
			PositionVal,
			NameVal
		}

		private readonly CtorType ctorType;
		private readonly int position;
		private readonly char val;
		private readonly String name;

		private SetCharacterEvent(CtorType ctorType, int position, char val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetCharacterEvent(int position, char val)
			: this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetCharacterEvent(String name, char val)
			: this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetCharacter(position, val);
					break;
				case CtorType.NameVal:
					query.SetCharacter(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetCharacterEvent: " + ctorType);
			}
		}
	}
}
