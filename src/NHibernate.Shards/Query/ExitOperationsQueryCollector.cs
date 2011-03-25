using System;
using System.Collections;
using NHibernate.Engine;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Query
{
	public class ExitOperationsQueryCollector : IExitOperationsCollector
	{
		// maximum number of results requested by the client
		private int? maxResults = null;

		// index of the first result requested by the client
		private int? firstResult = null;

		public IList Apply(IList result)
		{
			if (firstResult.HasValue)
			{
				result = new FirstResultExitOperation(firstResult.Value).Apply(result);
			}
			if (maxResults.HasValue)
			{
				result = new MaxResultsExitOperation(maxResults.Value).Apply(result);
			}

			return result;
		}

		public void SetSessionFactory(ISessionFactoryImplementor sessionFactoryImplementor)
		{
			throw new NotSupportedException();
		}

		public IExitOperationsCollector SetMaxResults(int maxResults)
		{
			this.maxResults = maxResults;
			return this;
		}

		public IExitOperationsCollector SetFirstResult(int firstResult)
		{
			this.firstResult = firstResult;
			return this;
		}
	}
}
