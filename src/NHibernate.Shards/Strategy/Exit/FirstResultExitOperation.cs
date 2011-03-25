using System;
using System.Collections;
using System.Collections.Generic;

namespace NHibernate.Shards.Strategy.Exit
{
	public class FirstResultExitOperation : IExitOperation
	{
		private readonly int firstResult;

		public FirstResultExitOperation(int firstResult)
		{
			this.firstResult = firstResult;
		}

		public IList Apply(IList results)
		{
			IList nonNullResults = ExitOperationUtils.GetNonNullList(results);
			if (nonNullResults.Count <= firstResult)
			{
				return new List<object>();
			}
			return ExitOperationUtils.GetSubList(nonNullResults, firstResult, nonNullResults.Count - 1);
		}
	}
}
