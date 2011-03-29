using System.Collections.Generic;
using System.Threading;

namespace NHibernate.Shards.LoadBalance
{

    /// <summary>
    /// Round robin load balancing algorithm.
    /// </summary>
    public class RoundRobinShardLoadBalancer : BaseShardLoadBalancer
    {
        private int lastIndex = -1;

        /// <summary>
        /// Construct a RoundRobinShardLoadBalancer
        /// </summary>
        /// <param name="shardIds">the ShardIds that we're balancing across</param>
        public RoundRobinShardLoadBalancer(IEnumerable<ShardId> shardIds)
            : base(shardIds)
        { }

        /// <summary>
        /// The index of the next ShardId we should return
        /// </summary>
        protected override int NextIndex
        {
            get { return Interlocked.Increment(ref lastIndex) % ShardIds.Count; }
        }
    }
}