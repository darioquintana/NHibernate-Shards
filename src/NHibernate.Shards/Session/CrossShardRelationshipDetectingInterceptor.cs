using System;
using System.Collections.Generic;
using log4net;
using NHibernate.Shards.Util;
using NHibernate.Type;

namespace NHibernate.Shards.Session
{
	public class CrossShardRelationshipDetectingInterceptor : EmptyInterceptor
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (CrossShardRelationshipDetectingInterceptor));
		private readonly IShardIdResolver shardIdResolver;

		public CrossShardRelationshipDetectingInterceptor(IShardIdResolver shardIdResolver)
		{
			Preconditions.CheckNotNull(shardIdResolver);
			this.shardIdResolver = shardIdResolver;
		}

		public override bool OnFlushDirty(
			object entity,
			object id,
			object[] currentState,
			object[] previousState,
			string[] propertyNames,
			IType[] types)
		{
			//ShardId expectedShardId = GetAndRefreshExpectedShardId(entity);
			//Preconditions.CheckNotNull(expectedShardId);

			//IList<ICollection<object>> collections = null;
			throw new NotImplementedException();
		}

	    public static List<KeyValuePair<IType, object>> BuildListOfAssociations(IType[] types, object[] currentState)
		{
            // we assume types and current state are the same length
            Preconditions.CheckState(types.Length == currentState.Length);
            var associationList = new List<KeyValuePair<IType, object>>();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] != null &&
                    currentState[i] != null &&
                    types[i].IsAssociationType)
                {
                    associationList.Add(new KeyValuePair<IType, object>());//Pair.of(types[i], currentState[i])
                }
            }
            return associationList;
		}

		private ShardId GetAndRefreshExpectedShardId(object @object)
		{
			ShardId expectedShardId = shardIdResolver.GetShardIdForObject(@object);
			if (expectedShardId == null)
			{
				expectedShardId = ShardedSessionImpl.CurrentSubgraphShardId;
			}
			else
			{
				ShardedSessionImpl.CurrentSubgraphShardId = expectedShardId;
			}
			return expectedShardId;
		}

	}
}