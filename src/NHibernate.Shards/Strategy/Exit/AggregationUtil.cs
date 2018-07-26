using System;
using System.Collections;

namespace NHibernate.Shards.Strategy.Exit
{
    public static class AggregationUtil
	{
	    public static object Max(this IEnumerable items, Func<object, object> maxSelector)
	    {
	        IComparable result = null;
	        foreach (var item in items)
	        {
	            var max = maxSelector(item) as IComparable;
	            if (result == null)
	            {
	                result = max;
	            }
	            else if (max != null)
	            {
	                if (result.CompareTo(max) < 0)
	                {
	                    result = max;
	                }
	            }
	        }
	        return result;
	    }

        public static object Min(this IEnumerable items, Func<object, object> minSelector)
	    {
	        IComparable result = null;
	        foreach (var item in items)
	        {
	            var min = minSelector(item) as IComparable;
	            if (result == null)
	            {
	                result = min;
	            }
	            else if (min != null)
	            {
	                if (result.CompareTo(min) > 0)
	                {
	                    result = min;
	                }
	            }
	        }
	        return result;
	    }

	    public static object SumInt64(this IEnumerable items, Func<object, object> sumSelector)
	    {
	        long sumTotal = 0;

	        foreach (var result in items)
	        {
	            var sum = sumSelector(result);
	            if (sum != null)
	            {
	                sumTotal += Convert.ToInt64(sum);
	            }
	        }

	        return sumTotal;
	    }

        public static object Sum(this IEnumerable items, Func<object, object> sumSelector)
	    {
	        double sumTotal = 0;

	        foreach (var result in items)
	        {
	            var sum = sumSelector(result);
	            if (sum != null)
	            {
	                sumTotal += Convert.ToDouble(sum);
	            }
	        }

	        return sumTotal;
	    }

        public static object Average(this IEnumerable items, Func<object, object> avgSelector, Func<object, object> countSelector)
		{
			double sumTotal = 0;
			long countTotal = 0;

			foreach (var result in items)
			{
				var avg = avgSelector(result);
				var count = countSelector(result);
				if (avg != null && count != null)
				{
				    var countInt64 = Convert.ToInt64(count);
					sumTotal += Convert.ToDouble(avg) * countInt64;
					countTotal += countInt64;
				}
			}

			return countTotal > 0
				? sumTotal / countTotal
				: default(double?);
		}
	}
}
