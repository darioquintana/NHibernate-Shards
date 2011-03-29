namespace NHibernate.Shards
{
    using System;

    /// <summary>
	/// Uniquely identifies a virtual shard.
	/// </summary>
	public class ShardId: IEquatable<ShardId>
	{
		private readonly short shardId;

		public ShardId(short shardId)
		{
			this.shardId = shardId;
		}

		public short Id
		{
			get { return shardId; }
		}

        public static bool operator ==(ShardId left, ShardId right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;

            return left.shardId == right.shardId;
        }

        public static bool operator !=(ShardId left, ShardId right)
        {
            return !(left == right);
        }

        public bool Equals(ShardId other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
		{
            return this == obj as ShardId;
		}

		public override int GetHashCode()
		{
			return shardId;
		}

		public override string ToString()
		{
			return shardId.ToString();
		}
	}
}