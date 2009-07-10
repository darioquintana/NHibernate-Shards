using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Iesi.Collections.Generic;
using NHibernate.Shards.Util;
using NUnit.Framework;

namespace NHibernate.Shards.Test.Util
{
	[TestFixture]
	public class IterablesFixture
	{
		private Set<ShardId> GetShardIdSet()
		{
			Set<ShardId> hashedSet = new HashedSet<ShardId>();
			hashedSet.Add(new ShardId(1));
			hashedSet.Add(new ShardId(2));
			hashedSet.Add(new ShardId(3));
			return hashedSet;
		}

		[Test]
		public void ShouldIterateThroughSubItems()
		{
			var dic = new Dictionary<string, Set<ShardId>>();
			dic.Add("1", GetShardIdSet());
			dic.Add("2", GetShardIdSet());
			dic.Add("3", GetShardIdSet());

			Assert.AreEqual(3, dic.Count);

			IEnumerable iterable = dic.Values.Concatenation();

			iterable.AsQueryable().Cast<ShardId>().Count().Should().Be.EqualTo(9);

			//Can iterate
			foreach (object item in iterable)
			{
			}
		}
	}
}