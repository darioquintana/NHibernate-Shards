using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Criterion;

namespace NHibernate.Shards.Strategy.Exit
{
	public class DistinctExitOperation : IExitOperation
	{
	    private IProjection distinct;

        public DistinctExitOperation(IProjection distinct)
        {
            this.distinct = distinct;
            throw new NotSupportedException();
        }

		public IList Apply(IList results)
		{
		    IList uniqueSet = new List<object>();
            
		    foreach(object t in ExitOperationUtils.GetNonNullList(results))
		    {
		        if(!uniqueSet.Contains(t))
		        {
		            uniqueSet.Add(t);
		        }
		    }
		    return uniqueSet;
		}
	}
}