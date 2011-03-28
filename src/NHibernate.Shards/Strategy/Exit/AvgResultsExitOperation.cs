using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Shards.Threading.Exception;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Exit
{
	/// <summary>
	/// Performs post-processing on a result set that has had an average projection
	/// applied.
	///
	/// This may not yield the exact same result as you'd get if you ran the query
	/// on a single shard because there seems to be some platform-specific wiggle.
	/// Here's a specific example:
	/// On hsqldb, if you have a column of type DECIMAL(10, 4) and you ask for the
	/// average of the values in that column, you get the floor of the result.
	/// On MySQL, if you have a column of the same type, you get a result back with
	/// the expected precision.  So, um, just be careful.
	/// </summary>
	public class AvgResultsExitOperation : IExitOperation
	{
		private static readonly IInternalLogger Log = LoggerProvider.LoggerFor(typeof(AvgResultsExitOperation));

		#region IExitOperation Members

		public IList Apply(IList results)
		{
			IList nonNullResults = ExitOperationUtils.GetNonNullList(results);
			Double? total = null;
			int numResults = 0;
			foreach (Object result in nonNullResults)
			{

				// We expect all entries to be Object arrays.
				// the first entry in the array is the average (a double)
				// the second entry in the array is the number of rows that were examined
				// to arrive at the average.
				Pair<Double?, Int32?> pair = GetResultPair(result);
				Double? shardAvg = pair.first;
				if (shardAvg == null)
				{
					// if there's no result from this shard it doesn't go into the
					// calculation.  This is consistent with how avg is implemented
					// in the database
					continue;
				}
				int? shardResults = pair.second;
				Double? shardTotal = shardAvg * shardResults;
				if (total == null)
				{
					total = shardTotal;
				}
				else
				{
					total += shardTotal;
				}
				
				numResults += shardResults ?? 0;
			}
			if (numResults == 0 || total == null)
			{
				return new List<object> {null};
			}
			return new List<object> { total / numResults };
		}

		#endregion


		private Pair<Double?, Int32?> GetResultPair(Object result)
		{
			if (!(result is Object[]))
			{
				String msg = "Wrong type in result list. Expected " + typeof(Object[]) +
						" but found " + result.GetType();
				Log.Error(msg);
				throw new IllegalStateException(msg);
			}
			Object[] resultArr = (Object[])result;
			if (resultArr.Length != 2)
			{
				String msg =
					"Result array is wrong size. Expected 2 " +
						" but found " + resultArr.Length;
				Log.Error(msg);
				throw new IllegalStateException(msg);
			}
			return Pair<Double?, Int32?>.Of((Double?)resultArr[0], (Int32?)resultArr[1]);
		}
	}
}