namespace NHibernate.Shards.Query
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
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
	    private readonly IQueryExpressionPlan unshardedQueryExpressionPlan;
		private readonly ExitOperationBuilder exitOperationBuilder;
		private readonly Dictionary<string, Tuple<object, IType>> parameterValuesByName;
		private string key;

		public ShardedQueryExpression(IQueryExpressionPlan unshardedQueryExpressionPlan, ExitOperationBuilder exitOperationBuilder)
	    {
	        Preconditions.CheckNotNull(unshardedQueryExpressionPlan);
	        this.unshardedQueryExpressionPlan = unshardedQueryExpressionPlan;
		    this.exitOperationBuilder = exitOperationBuilder;

		    this.parameterValuesByName = unshardedQueryExpressionPlan.QueryExpression is NhLinqExpression linqExpression
			    ? new Dictionary<string, Tuple<object, IType>>(linqExpression.ParameterValuesByName)
			    : new Dictionary<string, Tuple<object, IType>>();
	    }

	    public string Key
		{
			get { return this.key ?? (this.key = "(Sharded)" + this.UnshardedQueryExpression.Key); }
		}

		public System.Type Type
		{
			get { return this.UnshardedQueryExpression.Type; }
		}

		public IList<NamedParameterDescriptor> ParameterDescriptors
		{
			get { return this.UnshardedQueryExpression.ParameterDescriptors; }
		}

		public IDictionary<string, Tuple<object, IType>> ParameterValuesByName
		{
			get { return this.parameterValuesByName; }
		}

		internal IQueryExpression UnshardedQueryExpression
		{
			get { return this.unshardedQueryExpressionPlan.QueryExpression; }
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
			var root = this.UnshardedQueryExpression.Translate(sessionFactory, filter);

		    var entityName = sessionFactory.TryGetGuessEntityName(this.Type);
		    if (entityName == null)
		    {
			    if (this.unshardedQueryExpressionPlan.ReturnMetadata.ReturnTypes[0] is EntityType rootEntityType)
		        {
		            entityName = rootEntityType.GetAssociatedEntityName(sessionFactory);

		        }
            }

		    var rootClassMetadata = entityName != null
		        ? sessionFactory.GetClassMetadata(entityName)
		        : null;
			var hqlGenerator = new ShardedHqlGenerator(root, rootClassMetadata, this.parameterValuesByName, this.exitOperationBuilder);
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
			var unfinishedCopies = new Stack<UnfinishedNodeCopy>();
			unfinishedCopies.Push(new UnfinishedNodeCopy(unshardedHql, null));

			while (true)
			{
				var unfinishedCopy = unfinishedCopies.Peek();
				if (!unfinishedCopy.Children.MoveNext())
				{
                    unfinishedCopy.Complete();
					unfinishedCopies.Pop();
					if (unfinishedCopies.Count == 0) return unfinishedCopy.Node;
					continue;
				}

				var nextChild = unfinishedCopy.Children.Current;
			    Action<IASTNode> copyTransformer;
				if (CanCopy(nextChild, out copyTransformer))
				{
					if (nextChild.ChildCount == 0)
					{
					    var nextChildCopy = nextChild.DupNode();
					    unfinishedCopy.Node.AddChild(nextChildCopy);
					    copyTransformer?.Invoke(nextChildCopy);
					}
					else
					{
						var unfinishedChildCopy = new UnfinishedNodeCopy(nextChild, copyTransformer);
						unfinishedCopy.Node.AddChild(unfinishedChildCopy.Node);
						unfinishedCopies.Push(unfinishedChildCopy);
					}
				}
			}
		}

		private bool CanCopy(IASTNode node, out Action<IASTNode> copyTransformer)
		{
		    copyTransformer = null;

			IASTNode child;
			switch (node.Type)
			{
				case HqlSqlWalker.SKIP:
					child = node.GetFirstChild();

					if (TryGetParameterName(child, out var skipParameterName))
					{
						this.exitOperationBuilder.FirstResult = (int)namedParameters[skipParameterName].Item1;
					}
					else if (TryGetInt32(child, out var firstResult))
					{										  
						this.exitOperationBuilder.FirstResult = firstResult;
					}
					return false;

				case HqlSqlWalker.TAKE:
					child = node.GetFirstChild();

					if (TryGetParameterName(child, out var takeParameterName))
					{
						this.exitOperationBuilder.MaxResults = (int)namedParameters[takeParameterName].Item1;
					}
					else if (TryGetInt32(child, out var maxResults))
					{
						this.exitOperationBuilder.MaxResults = maxResults;
					}
					return false;

				case HqlSqlWalker.ORDER:
					ExtractOrders(node);
					return true;

			    case HqlSqlWalker.AGGREGATE:
			        if ("avg".Equals(node.Text, StringComparison.OrdinalIgnoreCase))
			        {
			            goto case HqlSqlWalker.AVG;
			        }
                    if ("min".Equals(node.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        goto case HqlSqlWalker.MIN;
                    }
                    else if ("max".Equals(node.Text, StringComparison.OrdinalIgnoreCase))
			        {
			            goto case HqlSqlWalker.MAX;
			        }

                    throw new NotSupportedException(string.Format(
                        CultureInfo.InvariantCulture,
                        "HQL aggregate function '{0}' is currently not supported across shards.",
                        node.Text));

			    case HqlSqlWalker.AVG:
			        ThrowIfAggregationInComplexSelectList(node);

			        this.exitOperationBuilder.Aggregation = c => AggregationUtil.Average(c, GetFieldSelector(0), GetFieldSelector(1));
			        copyTransformer = TransformUnshardedAverageNode;
			        return true;
				case HqlSqlWalker.COUNT:
				    ThrowIfAggregationInComplexSelectList(node);
					this.exitOperationBuilder.Aggregation = c => AggregationUtil.SumInt64(c, o => o);
					return true;
			    case HqlSqlWalker.MIN:
			        ThrowIfAggregationInComplexSelectList(node);
                    this.exitOperationBuilder.Aggregation = c => AggregationUtil.Min(c, o => o);
			        return true;
			    case HqlSqlWalker.MAX:
			        ThrowIfAggregationInComplexSelectList(node);
			        this.exitOperationBuilder.Aggregation = c => AggregationUtil.Max(c, o => o);
			        return true;
				case HqlSqlWalker.SUM:
				    ThrowIfAggregationInComplexSelectList(node);
					this.exitOperationBuilder.Aggregation = c => AggregationUtil.Sum(c, o => o);
					return true;
			}

			return true;
		}

	    private void TransformUnshardedAverageNode(IASTNode average)
	    {
	        var star = average.DupNode();
	        star.Type = HqlSqlWalker.ROW_STAR;
	        star.Text = "*";

	        var count = average.DupNode();
	        count.Type = HqlSqlWalker.COUNT;
	        count.Text = "count";
            count.AddChild(star);

            while (average.Parent.Type != HqlSqlWalker.SELECT)
            {
	            average = average.Parent;
            }
            average.AddSibling(count);
	    }

	    private Func<object, object> GetFieldSelector(int fieldIndex)
	    {
            return o => o is object[] array && array.Length > fieldIndex
	            ? array[fieldIndex]
	            : null;
	    }

	    private static void ThrowIfAggregationInComplexSelectList(IASTNode node)
	    {
	        var parent = node.Parent;
	        while (parent != null)
	        {
	            if (parent.Type == HqlSqlWalker.SELECT)
	            {
	                if (parent.ChildCount == 1) return;
                    throw new NotSupportedException(
                        "HQL aggregation function must be only expression in select list. Complex " +
                        "select lists are not supported with aggregation functions across shards.");
	            }
	            parent = parent.Parent;
	        }
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
		    if (this.rootClassMetadata == null)
		    {
		        throw new NotSupportedException("Ordering of sharded scalar HQL queries is not supported");
		    }

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
						o => propertyPath == this.rootClassMetadata.IdentifierPropertyName
						    ? this.rootClassMetadata.GetIdentifier(o)
						    : this.rootClassMetadata.GetPropertyValue(o, propertyPath), 
						isDescending));
			}
		}

	    private class UnfinishedNodeCopy
	    {
            public IASTNode Node { get; }
	        public IEnumerator<IASTNode> Children { get; }
            public Action<IASTNode> Transformer { get; }

	        public UnfinishedNodeCopy(IASTNode node, Action<IASTNode> transformer)
	        {
	            this.Node = node.DupNode();
	            this.Children = GetChildren(node);
	            this.Transformer = transformer;
	        }

	        public void Complete()
	        {
		        Transformer?.Invoke(this.Node);
	        }

	        private static IEnumerator<IASTNode> GetChildren(IASTNode parent)
	        {
	            for (var i = 0; i < parent.ChildCount; i++) yield return parent.GetChild(i);
	        }
	    }
    }
}