using System;
using System.Collections;
using System.Collections.Generic;

namespace NHibernate.Shards.Util
{
	public static class Iterables
	{
		public static IEnumerable Concatenation<T>(this IEnumerable<T> iterables) where T : IEnumerable
		{
			foreach (T iterable in iterables)
			{
				foreach (object item in iterable)
				{
					yield return item;
				}
			}
		}

		public static void Each<T>(this IEnumerable<T> iterables, Action<T> action)
		{
			foreach (T item in iterables)
			{
				action(item);
			}
		}
	}
}