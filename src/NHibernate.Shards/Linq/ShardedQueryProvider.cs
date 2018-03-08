namespace NHibernate.Shards.Linq
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using NHibernate.Engine;
	using NHibernate.Impl;
	using NHibernate.Linq;
	using NHibernate.Type;

	public class ShardedQueryProvider : DefaultQueryProvider
	{
		public ShardedQueryProvider(ISessionImplementor session) : base(session)
		{}

		protected override NhLinqExpression PrepareQuery(Expression expression, out IQuery query)
		{
			// Detect non-sharded NHibernate session
			if (this.Session is SessionImpl) return base.PrepareQuery(expression, out query);

			var linqExpression = new NhLinqExpression(expression, this.Session.Factory);
			query = this.Session.CreateQuery(linqExpression);

			SetParameters(query, linqExpression.ParameterValuesByName);
			SetResultTransformerAndAdditionalCriteria(query, linqExpression, linqExpression.ParameterValuesByName);

			return linqExpression;
		}

		private static void SetParameters(IQuery query, IDictionary<string, Tuple<object, IType>> parameters)
		{
			foreach (string namedParameter in query.NamedParameters)
			{
				Tuple<object, IType> parameter = parameters[namedParameter];
				if (parameter.Item1 == null)
				{
					if (typeof(IEnumerable).IsAssignableFrom(parameter.Item2.ReturnedClass) 
						&& parameter.Item2.ReturnedClass != typeof(string))
					{
						query.SetParameterList(namedParameter, null, parameter.Item2);
					}
					else
					{
						query.SetParameter(namedParameter, null, parameter.Item2);
					}
				}
				else if (parameter.Item1 is IEnumerable && !(parameter.Item1 is string))
				{
					query.SetParameterList(namedParameter, (IEnumerable) parameter.Item1);
				}
				else if (parameter.Item2 != null)
				{
					query.SetParameter(namedParameter, parameter.Item1, parameter.Item2);
				}
				else
				{
					query.SetParameter(namedParameter, parameter.Item1);
				}
			}
		}
	}
}
