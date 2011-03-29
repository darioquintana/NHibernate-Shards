using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NHibernate.Shards.Util;

namespace NHibernate.Shards
{
    /// <summary>
	/// Base implementation for HasShadIdList.
	/// Takes care of null/empty checks.
	/// </summary>
	public abstract class BaseHasShardIdList : IHasShardIdList
	{
		/// <summary>
		/// our list of <see cref="ShardId"/> objects
		/// </summary>
		private readonly IList<ShardId> shardIds;

		protected BaseHasShardIdList(IEnumerable<ShardId> shardIds)
		{
			Preconditions.CheckNotNull(shardIds);

		    var shardIdList = shardIds.ToList();
            Preconditions.CheckArgument(shardIdList.Count > 0); //not empty
            
            this.shardIds = new ReadOnlyCollection<ShardId>(shardIdList);
        }

		protected BaseHasShardIdList()
		{}

		/// <summary>
		/// Unmodifiable list of <see cref="ShardId"/>s.
		/// </summary>
		public IList<ShardId> ShardIds
		{
			get { return shardIds; }
		}
	}
}