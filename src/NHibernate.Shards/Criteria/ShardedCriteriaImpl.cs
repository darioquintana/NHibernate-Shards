using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NHibernate.Criterion;
using NHibernate.Shards.Strategy.Access;
using NHibernate.Shards.Strategy.Exit;
using NHibernate.SqlCommand;
using NHibernate.Transform;

namespace NHibernate.Shards.Criteria
{
	/**
	 * Concrete implementation of the {@link ShardedCriteria} interface.
	 *
	 * @author maxr@google.com (Max Ross)
	 */
    public class ShardedCriteriaImpl:IShardedCriteria
    {
		// unique id for this ShardedCriteria
        private CriteriaId criteriaId;

		// the shards we know about
        private IList<IShard> shards;

		// a factory that knows how to create actual Criteria objects
        private ICriteriaFactory criteriaFactory;

		// the shard access strategy we use when we execute the Criteria
		// across multiple shards
        private IShardAccessStrategy shardAccessStrategy;

		// the criteria collector we use to process the results of executing
		// the Criteria across multiple shards
        private ExitOperationsCriteriaCollector criteriaCollector;

		/**
		 * Construct a ShardedCriteriaImpl
		 *
		 * @param criteriaId unique id for this ShardedCriteria
		 * @param shards the shards that this ShardedCriteria is aware of
		 * @param criteriaFactory factory that knows how to create concrete {@link Criteria} objects
		 * @param shardAccessStrategy the access strategy we use when we execute this
		 * ShardedCriteria across multiple shards.
		 */
        public ShardedCriteriaImpl(CriteriaId criteriaId,IList<IShard> shards, ICriteriaFactory criteriaFactory,IShardAccessStrategy shardAccessStrategy)
        {
            this.criteriaId = criteriaId;
            this.shards = shards;
            this.criteriaFactory = criteriaFactory;
            this.shardAccessStrategy = shardAccessStrategy;
            this.criteriaCollector = new ExitOperationsCriteriaCollector();
            criteriaCollector.SetSessionFactory(shards[0].SessionFactoryImplementor);
        }

        
        public CriteriaId CriteriaId
        {
            get { return this.criteriaId; }
        }

        public ICriteriaFactory CriteriaFactory
        {
            get { return this.criteriaFactory; }
        }

		/**
		  * @return any Criteria, or null if we don't have one
		  */
        private ICriteria GetSomeCriteria()
        {
            foreach(IShard shard in shards)
            {
                ICriteria crit = shard.GetCriteriaById(criteriaId);
                if(crit != null)
                {
                    return crit;
                }
            }
            return null;
        }

		/**
		 * @return any Criteria.  If no Criteria has been established we establish
		 * one and return it.
		 */
        private ICriteria GetOrEstablishSomeCriteria()
        {
            ICriteria crit = GetSomeCriteria();
            if(crit != null)
            {
                IShard shard = shards[0];
                crit = shard.EstablishCriteria(this);
            }
            return crit;
        }

        public string GetAlias()
        {
        	return GetOrEstablishSomeCriteria().Alias;
        }

        public ICriteria SetProjection(IProjection projectionValue)
        {
            criteriaCollector.AddProjection(projectionValue);
            if(projectionValue.GetType().IsAssignableFrom(typeof(AvgProjection)))
            {
                SetAvgProjection(projectionValue);
            }
			//TODO - handle ProjectionList
            return this;
        }

        private void SetAvgProjection(IProjection projection)
        {
			// We need to modify the query to pull back not just the average but also
			// the count.  We'll do this by creating a ProjectionList with both the
			// average and the row count.
            ProjectionList projectionList = Projections.ProjectionList();
            projectionList.Add(projection);
            projectionList.Add(Projections.RowCount());
            ICriteriaEvent criteriaEvent = new SetProjectionEvent(projectionList);
            foreach(IShard shard in shards)
            {
                if(shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetProjection(projectionList);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
        }

        public ICriteria SetProjection(params IProjection[] projection)
        {
            throw new NotSupportedException();
        }

        public ICriteria Add(ICriterion criterion)
        {
            ICriteriaEvent criteriaEvent = new AddCriterionEvent(criterion);
            foreach(IShard shard in shards)
            {
                if(shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).Add(criterion);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria AddOrder(Order order)
        {
            criteriaCollector.AddOrder(order);
            return this;
        }

        public ICriteria SetFetchMode(string associationPath, FetchMode fetchMode)
        {
            ICriteriaEvent criteriaEvent = new SetFetchModeEvent(associationPath, fetchMode);
            foreach(IShard shard in shards)
            {
                if(shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetFetchMode(associationPath, fetchMode);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetLockMode(LockMode lockMode)
        {
            ICriteriaEvent criteriaEvent = new SetLockModeEvent(lockMode);
            foreach(IShard shard in shards)
            {
                if(shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetLockMode(lockMode);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetLockMode(string alias,LockMode lockMode)
        {
            ICriteriaEvent criteriaEvent = new SetLockModeEvent(lockMode, alias);
            foreach (IShard shard in shards)
            {
                if (shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetLockMode(alias,lockMode); //fixed
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria CreateAlias(string associationPath, string alias)
        {
            ICriteriaEvent criteriaEvent = new CreateAliasEvent(associationPath, alias);
            foreach(IShard shard in shards)
            {
                if(shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).CreateAlias(associationPath, alias);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType)
        {
            ICriteriaEvent criteriaEvent = new CreateAliasEvent(associationPath, alias, joinType);
            foreach (IShard shard in shards)
            {
                if (shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).CreateAlias(associationPath, alias,joinType);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        private static readonly IEnumerable<ICriteriaEvent> NoCriteriaEvents =
            new ReadOnlyCollection<ICriteriaEvent>(new List<ICriteriaEvent>());

		/**
		 * Creating sharded subcriteria is tricky.  We need to give the client a
		 * reference to a ShardedSubcriteriaImpl (which to the client just looks like
		 * a Criteria object).  Then, for each shard where the Criteria has already been
		 * established we need to create the actual subcriteria, and for each shard
		 * where the Criteria has not yet been established we need to register an
		 * event that will create the Subcriteria when the Criteria is established.
		 */
        private ShardedSubcriteriaImpl CreateSubcriteria(ISubcriteriaFactory factory)
        {
            var subCrit = new ShardedSubcriteriaImpl(shards, this);
            foreach(IShard shard in shards)
            {
                ICriteria crit = shard.GetCriteriaById(criteriaId);
                if(crit != null)
                {
                    factory.CreateSubcriteria(crit, NoCriteriaEvents);
                }
                else
                {
                    var subCriteriaEvent = new CreateSubcriteriaEvent(factory,
                                                                                         subCrit.SubcriteriaRegistrar(
                                                                                             shard),
                                                                                         subCrit.ShardToCriteriaMap,
                                                                                         subCrit.ShardToEventListMap);
                    shard.AddCriteriaEvent(criteriaId,subCriteriaEvent);
                }
            }
            return subCrit;
        }
        
        public ICriteria CreateCriteria(string associationPath)
        {
            ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath);
            return CreateSubcriteria(factory);
        }

        public ICriteria CreateCriteria(string associationPath, JoinType joinType)
        {
            ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, joinType);
            return CreateSubcriteria(factory);
        }

        public ICriteria CreateCriteria(string associationPath, string alias)
        {
            ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, alias);
            return CreateSubcriteria(factory);
        }

        public ICriteria CreateCriteria(string associationPath, string alias, JoinType joinType)
        {
            ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, alias, joinType);
            return CreateSubcriteria(factory);
        }

        public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType, ICriterion withClause)
        {
            throw new NotSupportedException();
        }

        public ICriteria CreateCriteria(string associationPath, string alias, JoinType joinType, ICriterion withClause)
        {
            throw new NotSupportedException();
        }

        public ICriteria SetResultTransformer(IResultTransformer resultTransformer)
        {
            ICriteriaEvent criteriaEvent = new SetResultTransformerEvent(resultTransformer);
            foreach(IShard shard in shards)
            {
                if(shard.GetCriteriaById(criteriaId)!= null)
                {
                    shard.GetCriteriaById(criteriaId).SetResultTransformer(resultTransformer);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId,criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetMaxResults(int maxResults)
        {
            criteriaCollector.MaxResults(maxResults);
            return this;
        }

        public ICriteria SetFirstResult(int firstResult)
        {
            criteriaCollector.FirstResult(firstResult);
            return this;
        }

        public ICriteria SetFetchSize(int fetchSize)
        {
            ICriteriaEvent criteriaEvent = new SetFetchSizeEvent(fetchSize);
            foreach(IShard shard in shards)
            {
                if(shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetFetchSize(fetchSize);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetTimeout(int timeout)
        {
            ICriteriaEvent criteriaEvent = new SetTimeoutEvent(timeout);
            foreach (IShard shard in shards)
            {
                if (shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetTimeout(timeout);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetCacheable(bool cacheable)
        {
            ICriteriaEvent criteriaEvent = new SetCacheableEvent(cacheable);
            foreach (IShard shard in shards)
            {
                if (shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetCacheable(cacheable);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetCacheRegion(string cacheRegion)
        {
            ICriteriaEvent criteriaEvent = new SetCacheRegionEvent(cacheRegion);
            foreach (IShard shard in shards)
            {
                if (shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetCacheRegion(cacheRegion);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetComment(string comment)
        {
            ICriteriaEvent criteriaEvent = new SetCommentEvent(comment);
            foreach (IShard shard in shards)
            {
                if (shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetComment(comment);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetFlushMode(FlushMode flushMode)
        {
            ICriteriaEvent criteriaEvent = new SetFlushModeEvent(flushMode);
            foreach (IShard shard in shards)
            {
                if (shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetFlushMode(flushMode);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetCacheMode(CacheMode cacheMode)
        {
            ICriteriaEvent criteriaEvent = new SetCacheModeEvent(cacheMode);
            foreach (IShard shard in shards)
            {
                if (shard.GetCriteriaById(criteriaId) != null)
                {
                    shard.GetCriteriaById(criteriaId).SetCacheMode(cacheMode);
                }
                else
                {
                    shard.AddCriteriaEvent(criteriaId, criteriaEvent);
                }
            }
            return this;
        }

        //public ScrollableResults Scroll()
        //{
        //    throw new NotSupportedException();
        //}

        //public ScrollableResults Scroll(ScrollMode scrollMode)
        //{
        //    throw new NotSupportedException();
        //}

        public IList List()
        {
			//build a shard operation and apply it across all shards
            IShardOperation<IList> shardOp = new ListShardOperation<IList>(this);
            IExitStrategy<IList> exitStrategy = new ConcatenateListsExitStrategy();

			/**
			 * We don't support shard selection for criteria queries.  If you want
			 * custom shards, create a ShardedSession with only the shards you want.
			 * We're going to concatenate all our results and then use our
			 * criteria collector to do post processing.
			 */
            return this.shardAccessStrategy.Apply(this.shards, shardOp, exitStrategy,
                                                                    this.criteriaCollector);
        }

        public Object UniqueResult()
        {
			// build a shard operation and apply it across all shards
            IShardOperation<object> shardOp = new UniqueResultShardOperation<object>(this);
            IExitStrategy<object> exitStrategy = new FirstNonNullResultExitStrategy<object>();
			/**
			 * We don't support shard selection for criteria queries.  If you want
			 * custom shards, create a ShardedSession with only the shards you want.
			 * We're going to return the first non-null result we get from a shard.
			 */
            return this.shardAccessStrategy.Apply(this.shards, shardOp, exitStrategy, this.criteriaCollector);
        }

        public IEnumerable<T> Future<T>()
        {
            throw new NotSupportedException();
        }

        public IFutureValue<T> FutureValue<T>()
        {
            throw new NotSupportedException();
        }

        public void List(IList results)
        {
            throw new NotSupportedException();
        }

        public IList<T> List<T>()
        {
            throw new NotSupportedException();
        }

        public T UniqueResult<T>()
        {
            throw new NotSupportedException();
        }

        public void ClearOrders()
        {
            throw new NotSupportedException();
        }

        public ICriteria GetCriteriaByPath(string path)
        {
            throw new NotSupportedException();
        }

        public ICriteria GetCriteriaByAlias(string alias)
        {
            throw new NotSupportedException();
        }

        public System.Type GetRootEntityTypeIfAvailable()
        {
            throw new NotSupportedException();
        }

        public string Alias
        {
			get { return GetOrEstablishSomeCriteria().Alias; }
        }

        public object Clone()
        {
            throw new NotSupportedException();
        }
    }
}
