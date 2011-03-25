using System;

namespace NHibernate.Shards.Util
{
	public class Pair<TKey, TValue>
	{
		public TKey first;

		public TValue second;

		private Pair(TKey first, TValue second)
		{
			this.first = first;
			this.second = second;
		}

		public TKey First
		{
			get { return first; }
		}

		public TValue Second
		{
			get { return second; }
		}

		public static Pair<TKey, TValue> Of(TKey first, TValue second)
		{
			return new Pair<TKey, TValue>(first, second);
		}

		private static bool Eq(Object a, Object b)
		{
			return a == b || (a != null && a.Equals(b));
		}

		public override bool Equals(object obj)
		{
			if (typeof (object) == typeof (Pair<object, object>))
			{
				Pair<object, object> other = (Pair<object, object>) obj;
				return Eq(first, other.first) && Eq(second, other.second);
			}
			return false;
		}
	}
}