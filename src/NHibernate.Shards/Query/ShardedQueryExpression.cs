namespace NHibernate.Shards.Query
{
	using System;
	using System.Collections.Generic;
	using NHibernate.Engine;
	using NHibernate.Engine.Query;
	using NHibernate.Hql.Ast.ANTLR;
	using NHibernate.Hql.Ast.ANTLR.Tree;
	using NHibernate.Hql.Ast.ANTLR.Util;
	using NHibernate.Linq;
	using NHibernate.Metadata;
	using NHibernate.Shards.Strategy.Exit;
	using NHibernate.Shards.Util;
	using NHibernate.Type;

	public class ShardedQueryExpression : IQueryExpression
	{
		private readonly IQueryExpression unshardedQueryExpression;
		private readonly ExitOperationBuilder exitOperationBuilder;
		private readonly Dictionary<string, Tuple<object, IType>> parameterValuesByName;
		private string key;

		public ShardedQueryExpression(IQueryExpression unshardedQueryExpression, ExitOperationBuilder exitOperationBuilder)
	    {
	        Preconditions.CheckNotNull(unshardedQueryExpression);
	        this.unshardedQueryExpression = unshardedQueryExpression;
		    this.exitOperationBuilder = exitOperationBuilder;

		    var linqExpression = unshardedQueryExpression as NhLinqExpression;
		    this.parameterValuesByName = linqExpression != null
			    ? new Dictionary<string, Tuple<object, IType>>(linqExpression.ParameterValuesByName)
			    : new Dictionary<string, Tuple<object, IType>>();
	    }

	    public string Key
		{
			get { return this.key ?? (this.key = "(Sharded)" + this.unshardedQueryExpression.Key); }
		}

		public System.Type Type
		{
			get { return this.unshardedQueryExpression.Type; }
		}

		public IList<NamedParameterDescriptor> ParameterDescriptors
		{
			get { return this.unshardedQueryExpression.ParameterDescriptors; }
		}

		public IDictionary<string, Tuple<object, IType>> ParameterValuesByName
		{
			get { return this.parameterValuesByName; }
		}

		internal IQueryExpression UnshardedQueryExpression
		{
			get { return this.unshardedQueryExpression; }
		}

        /// <summary>
        /// This translation operation is called when a shard-aware HQL query is to be generated for 
        /// a shard that will execute the query. It transforms the shard-unaware query plan
        /// that may contain aggregations and paging instructions that will not work as expected
        /// when query results are to be aggregated from multiple shards.
        /// </summary>
        /// <param name="sessionFactory"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
		public IASTNode Translate(ISessionFactoryImplementor sessionFactory, bool filter)
		{
			var root = this.unshardedQueryExpression.Translate(sessionFactory, filter);

		    var entityName = sessionFactory.TryGetGuessEntityName(this.Type) ?? this.Type.FullName;
			var hqlGenerator = new ShardedHqlGenerator(root, sessionFactory.GetClassMetadata(entityName), 
				this.parameterValuesByName, this.exitOperationBuilder);
			return hqlGenerator.ShardedHql;
		}
	}

	internal class ShardedHqlGenerator
	{
		private readonly IClassMetadata rootClassMetadata;
		private readonly IASTNode shardedQuery;
		private readonly ExitOperationBuilder exitOperationBuilder;
		private readonly IDictionary<string, Tuple<object, IType>> namedParameters;

		public ShardedHqlGenerator(
			IASTNode unshardedHql, 
			IClassMetadata rootClassMetadata,
			IDictionary<string, Tuple<object, IType>> namedParameters,
			ExitOperationBuilder exitOperationBuilder)
		{
            this.rootClassMetadata = rootClassMetadata;
			this.exitOperationBuilder = exitOperationBuilder;
			this.namedParameters = namedParameters;
			this.shardedQuery = ToShardedHql(unshardedHql);
		}

		public IASTNode ShardedHql
		{
			get { return this.shardedQuery; }
		}

		private IASTNode ToShardedHql(IASTNode unshardedHql)
		{
			var unfinishedCopies = new Stack<KeyValuePair<IASTNode, IEnumerator<IASTNode>>>();

			unfinishedCopies.Push(GetUnfinishedCopy(unshardedHql));

			while (true)
			{
				var unfinishedCopy = unfinishedCopies.Peek();
				if (!unfinishedCopy.Value.MoveNext())
				{
					unfinishedCopies.Pop();
					if (unfinishedCopies.Count == 0) return unfinishedCopy.Key;
					continue;
				}

				var nextChild = unfinishedCopy.Value.Current;
				if (CanCopy(nextChild))
				{
					if (nextChild.ChildCount == 0)
					{
						unfinishedCopy.Key.AddChild(nextChild.DupNode());
					}
					else
					{
						var unfinishedChildCopy = GetUnfinishedCopy(nextChild);
						unfinishedCopy.Key.AddChild(unfinishedChildCopy.Key);
						unfinishedCopies.Push(unfinishedChildCopy);
					}
				}
			}
		}

		private bool CanCopy(IASTNode node)
		{
			IASTNode child;
			switch (node.Type)
			{
				case HqlSqlWalker.SKIP:
					child = node.GetFirstChild();

					string skipParameterName;
					int firstResult;
					if (TryGetParameterName(child, out skipParameterName))
					{
						this.exitOperationBuilder.FirstResult = (int)namedParameters[skipParameterName].Item1;
					}
					else if (TryGetInt32(child, out firstResult))
					{										  
						this.exitOperationBuilder.FirstResult = firstResult;
					}
					return false;

				case HqlSqlWalker.TAKE:
					child = node.GetFirstChild();

					string takeParameterName;
					int maxResults;
					if (TryGetParameterName(child, out takeParameterName))
					{
						this.exitOperationBuilder.MaxResults = (int)namedParameters[takeParameterName].Item1;
					}
					else if (TryGetInt32(child, out maxResults))
					{
						this.exitOperationBuilder.MaxResults = maxResults;
					}
					return false;

				case HqlSqlWalker.ORDER:
					ExtractOrders(node);
					return true;
				case HqlSqlWalker.AVG:
					// TODO: Replace AVG with SUM and COUNT 
					return true;
				case HqlSqlWalker.COUNT:
					// TODO: Determine aggregation operand type
					this.exitOperationBuilder.Aggregation = AggregationUtil.GetSumFunc(typeof(long));
					return true;
				case HqlSqlWalker.SUM:
					// TODO: Determine aggregation operand type
					this.exitOperationBuilder.Aggregation = AggregationUtil.GetSumFunc(typeof(long));
					return true;
			}

			return true;
		}

		private static bool TryGetParameterName(IASTNode node, out string result)
		{
			if (node.Type != HqlSqlWalker.COLON)
			{
				result = null;
				return false;
			}
			result = node.GetFirstChild().Text;
			return true;
		}

		private static bool TryGetInt32(IASTNode node, out int result)
		{
			if (node.Type != HqlSqlWalker.NUM_INT)
			{
				result = 0;
				return false;
			}
			result = int.Parse(node.Text);
			return true;
		}

		private void ExtractOrders(IASTNode node)
		{
			var child = node.GetFirstChild();
			while (child != null)
			{
				var propertyPath = ASTUtil.GetPathText(child);
				var aliasLength = propertyPath.IndexOf('.');
				if (aliasLength >= 0) propertyPath = propertyPath.Substring(aliasLength + 1);

				var isDescending = false;

				child = child.NextSibling;
				if (child != null)
				{
					isDescending = child.Type == HqlSqlWalker.DESCENDING;
					if (isDescending || child.Type == HqlSqlWalker.ASCENDING)
					{
						child = child.NextSibling;
					}
				}

				this.exitOperationBuilder.Orders.Add(
					new SortOrder(
						o => this.rootClassMetadata.GetPropertyValue(o, propertyPath), 
						isDescending));
			}
		}

		private static KeyValuePair<IASTNode, IEnumerator<IASTNode>> GetUnfinishedCopy(IASTNode node)
		{
			return new KeyValuePair<IASTNode, IEnumerator<IASTNode>>(node.DupNode(), GetChildren(node));
		}

		private static IEnumerator<IASTNode> GetChildren(IASTNode parent)
		{
			for (var i = 0; i < parent.ChildCount; i++) yield return parent.GetChild(i);
		}
	}
}