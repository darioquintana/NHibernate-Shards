using System;
using System.Collections.Generic;
using NHibernate;
using NHibernate.Shards.Criteria;

namespace nhibernate.shards.criteria
{
	public class Subcriteriafactoryimpl : ISubCriteriaFactory
	{
		public ICriteria CreateSubcriteria(ICriteria parent, IList<ICriteriaEvent> events)
		{
			throw new NotImplementedException();
		}
	}
}
