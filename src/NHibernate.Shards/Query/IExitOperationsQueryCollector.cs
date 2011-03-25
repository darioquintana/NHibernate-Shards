using System;
using System.Collections.Generic;
using NHibernate.Shards.Strategy.Exit;

namespace NHibernate.Shards.Query
{
	public interface IExitOperationsQueryCollector : IExitOperationsCollector
	{
		IList<Object> Apply(List<Object> result);
	}
}