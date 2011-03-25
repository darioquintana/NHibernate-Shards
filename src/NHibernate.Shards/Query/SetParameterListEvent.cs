using System;
using System.Collections;
using NHibernate.Shards.Session;
using NHibernate.Type;

namespace NHibernate.Shards.Query
{
	public class SetParameterListEvent : IQueryEvent
	{
		private readonly CtorType ctorType;
		private readonly String name;
		private readonly IType type;
		private readonly Object[] valsArr;
		private readonly ICollection valsColl;

		private SetParameterListEvent(CtorType ctorType, String name, ICollection valsColl, Object[] valsArr, IType type)
		{
			this.ctorType = ctorType;
			this.name = name;
			this.valsColl = valsColl;
			this.valsArr = valsArr;
			this.type = type;
		}

		public SetParameterListEvent(String name, ICollection vals, IType type) :
			this(CtorType.NameValsCollType, name, vals, null, type)
		{
		}

		public SetParameterListEvent(String name, ICollection vals) :
			this(CtorType.NameValsColl, name, vals, null, null)
		{
		}

		public SetParameterListEvent(String name, Object[] vals) :
			this(CtorType.NameValsObjArr, name, null, vals, null)
		{
		}

		public SetParameterListEvent(String name, Object[] vals, IType type) :
			this(CtorType.NameValsObjArrType, name, null, vals, type)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.NameValsCollType:
					query.SetParameterList(name, valsColl, type);
					break;
				case CtorType.NameValsColl:
					query.SetParameterList(name, valsColl);
					break;
				case CtorType.NameValsObjArr:
					query.SetParameterList(name, valsArr);
					break;
				case CtorType.NameValsObjArrType:
					query.SetParameterList(name, valsArr, type);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetParameterListEvent: " + ctorType);
			}
		}

		#region Nested type: CtorType

		private enum CtorType
		{
			NameValsCollType,
			NameValsColl,
			NameValsObjArr,
			NameValsObjArrType
		}

		#endregion
	}
}
