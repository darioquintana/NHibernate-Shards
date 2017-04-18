using System;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Exit
{
	public struct SortOrder : IEquatable<SortOrder>
	{
		private readonly Func<object, object> propertyGetter;
		private readonly bool isDescending;

		public SortOrder(Func<object, object> propertyGetter, bool isDescending)
		{
			Preconditions.CheckNotNull(propertyGetter);
			this.propertyGetter= propertyGetter;
			this.isDescending = isDescending;
		}

		public static SortOrder Ascending(string propertyName)
		{
			return new SortOrder(o => ListExitOperationUtils.GetPropertyValue(o, propertyName), false);
		}

		public static SortOrder Ascending(Func<object, object> propertyGetter)
		{
			return new SortOrder(propertyGetter, false);
		}

		public static SortOrder Descending(string propertyName)
		{
			return new SortOrder(o => ListExitOperationUtils.GetPropertyValue(o, propertyName), true);
		}

		public static SortOrder Descending(Func<object, object> propertyGetter)
		{
			return new SortOrder(propertyGetter, true);
		}

		public Func<object, object> PropertyGetter
		{
			get { return this.propertyGetter; }
		}

		public bool IsDescending
		{
			get { return this.isDescending; }
		}

		public bool Equals(SortOrder other)
		{
			return this.propertyGetter.Method == other.propertyGetter.Method
				&& this.isDescending == other.isDescending;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is SortOrder)) return false;
			return Equals((SortOrder)obj);
		}

		public override int GetHashCode()
		{
			return this.propertyGetter.Method.GetHashCode();
		}
	}
}
