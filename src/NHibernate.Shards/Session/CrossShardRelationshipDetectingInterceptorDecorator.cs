using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Session
{
	internal class CrossShardRelationshipDetectingInterceptorDecorator : InterceptorDecorator
    {
        private CrossShardRelationshipDetectingInterceptor csrdi;

		public CrossShardRelationshipDetectingInterceptorDecorator(CrossShardRelationshipDetectingInterceptor csrdi,
		                                                           IInterceptor delegateInterceptor)
			: base(delegateInterceptor)
		{
			this.csrdi = csrdi;
		}

		public override bool OnFlushDirty(object entity, object id, object[] currentState, object[] previousState,
		                                  string[] propertyNames, NHibernate.Type.IType[] types)
		{
			csrdi.OnFlushDirty(entity, id, currentState, previousState, propertyNames, types);
			return delegateInterceptor.OnFlushDirty(entity, id, currentState, previousState, propertyNames, types);
		}

		public override void OnCollectionUpdate(object collection, object key)
		{
			csrdi.OnCollectionUpdate(collection, key);
			delegateInterceptor.OnCollectionUpdate(collection, key);
		}

		private CrossShardRelationshipDetectingInterceptor CrossShardRelationshipDetectingInterceptor
		{
			get { return csrdi; }
		}
	}
}