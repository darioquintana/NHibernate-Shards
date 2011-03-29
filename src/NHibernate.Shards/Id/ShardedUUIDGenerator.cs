using System;
using System.Globalization;
using NHibernate.Id;
using NHibernate.Shards.Session;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Id
{
	/// <summary>
	/// TODO: documentation
	/// TODO: See if ShardedUUIDGenerator need to inherit from UUIDHexGenerator
	/// </summary>
	public class ShardedUUIDGenerator : UUIDHexGenerator, IShardEncodingIdentifierGenerator
	{
		public ShardId ExtractShardId(object identifier)
		{
			Preconditions.CheckNotNull(identifier);

			var id = (string)identifier;
			short shardId = short.Parse(id.Substring(0, 4), NumberStyles.AllowHexSpecifier);
			return new ShardId(shardId);
		}

		protected override string GenerateNewGuid()
		{
            short shardId = GetShardId();

            var g = Guid.NewGuid().ToByteArray();
            var a = (BitConverter.ToUInt32(g, 0) & 0x0000FFFF) | (uint)shardId << 16;
            var b = BitConverter.ToUInt16(g, 4);
            var c = BitConverter.ToUInt16(g, 8);
            return new Guid(a, b, c, g[8], g[9], g[10], g[11], g[12], g[13], g[14], g[15]).ToString(format);
		}

		private short GetShardId()
		{
			ShardId shardId = ShardedSessionImpl.CurrentSubgraphShardId;
			Preconditions.CheckState(shardId != null);
			return Convert.ToInt16(shardId.Id);
		}
	}
}