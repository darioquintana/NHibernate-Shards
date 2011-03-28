using System;
using NHibernate.Properties;

namespace NHibernate.Shards.Strategy.Exit
{
    public class ListExitOperationUtils
	{
		public static IComparable GetPropertyValue(object obj, string propertyName)
		{
			//TODO respect the client's choice in how Hibernate accesses property values.
			IGetter getter = new BasicPropertyAccessor().GetGetter(obj.GetType(), propertyName);

			return (IComparable) getter.Get(obj);
		}
	}
}