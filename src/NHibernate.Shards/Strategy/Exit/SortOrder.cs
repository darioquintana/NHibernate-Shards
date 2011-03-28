using System;

namespace NHibernate.Shards.Strategy.Exit
{
    public struct SortOrder : IEquatable<SortOrder>
    {
        private readonly string propertyName;
        private readonly bool isDescending;

        public SortOrder(string propertyName, bool isDescending)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("Property name must be specified.", "propertyName");
            }

            this.propertyName = propertyName;
            this.isDescending = isDescending;
        }

        public static SortOrder Ascending(string propertyName)
        {
            return new SortOrder(propertyName, false);
        }

        public static SortOrder Descending(string propertyName)
        {
            return new SortOrder(propertyName, true);
        }

        public string PropertyName
        {
            get { return this.propertyName; }
        }

        public bool IsDescending
        {
            get { return this.isDescending; }
        }

        public bool Equals(SortOrder other)
        {
            return this.propertyName == other.propertyName
                && this.isDescending == other.isDescending;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SortOrder)) return false;
            return Equals((SortOrder)obj);
        }

        public override int GetHashCode()
        {
            return this.propertyName.GetHashCode();
        }
    }
}
