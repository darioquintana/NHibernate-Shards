using System;
using NHibernate.Mapping;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetPropertiesEvent : IQueryEvent
	{
		private readonly Object bean;
		private readonly Map map;
		private readonly MethodSig sig;

		public SetPropertiesEvent(Object bean) :
			this(MethodSig.Object, bean, null)
		{
		}

		public SetPropertiesEvent(Map map) :
			this(MethodSig.Map, null, map)
		{
		}

		private SetPropertiesEvent(MethodSig sig, Object bean, Map map)
		{
			this.sig = sig;
			this.bean = bean;
			this.map = map;
		}

		public void OnEvent(IQuery query)
		{
			switch (sig)
			{
				case MethodSig.Object:
					query.SetProperties(bean);
					break;
				case MethodSig.Map:
					query.SetProperties(map);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown sig in SetPropertiesEvent: " + sig);
			}
		}

		#region Nested type: MethodSig

		private enum MethodSig
		{
			Object,
			Map
		}

		#endregion
	}
}
