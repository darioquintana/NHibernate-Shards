using System;
using System.Collections;
using NHibernate.Criterion;
using System.Collections.Generic;
using log4net;
using NHibernate.Engine;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Criteria
{
	public class ExitOperationsCriteriaCollector : IExitOperationsCollector
	{
		public IList Apply(IList result)
		{
			throw new NotImplementedException();
		}

		public void SetSessionFactory(ISessionFactoryImplementor sessionFactoryImplementor)
		{
			throw new NotImplementedException();
		}
	}
}
