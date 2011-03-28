using System.Collections;
using System.Collections.Generic;
using NHibernate.Criterion;
using NHibernate.Shards.Util;
using NHibernate.SqlCommand;
using NHibernate.Transform;

namespace NHibernate.Shards.Criteria
{
	/**
	 * Concrete implementation of the {@link ShardedSubcriteria} interface.
	 * You'll notice that this class does not extend {@link ShardedCriteria}.
	 * Why? Because {@link CriteriaImpl.Subcriteria} doesn't extend {@link Criteria}.  We
	 * don't actually need the entire {@link Criteria} interface.
	 *
	 * @author maxr@google.com (Max Ross)
	 */
    class ShardedSubcriteriaImpl : IShardedSubcriteria
    {
		// all shards that we're aware of
        private  IList<IShard> shards;

		// our parent. As with CriteriaImpl, we pass-through certain operations
		// to our parent
        private  IShardedCriteria parent;

		// maps shards to actual Criteria objects
        private  IDictionary<IShard, ICriteria> shardToCriteriaMap = new Dictionary<IShard, ICriteria>();

		// maps shards to lists of criteria events that need to be applied
		// when the actual Criteria objects are established
        private  IDictionary<IShard, IList<ICriteriaEvent>> shardToEventListMap =
            new Dictionary<IShard, IList<ICriteriaEvent>>();


		/**
		 * Construct a ShardedSubcriteriaImpl
		 *
		 * @param shards the shards that we're aware of
		 * @param parent our parent
		 */
        public ShardedSubcriteriaImpl(IList<IShard> shards, IShardedCriteria parent)
        {
            Preconditions.CheckNotNull(shards);
            Preconditions.CheckNotNull(parent);
            Preconditions.CheckArgument(!(shards.Count == 0));
            this.shards = shards;
            this.parent = parent;
			//let's set up our maps
            foreach (IShard shard in shards)
            {
                shardToCriteriaMap.Add(shard, null);
                shardToEventListMap.Add(shard, new List<ICriteriaEvent>());
            }
        }

        public object Clone()
        {
            return parent.Clone();
        }

        public T UniqueResult<T>()
        {
            return parent.UniqueResult<T>();
        }

        public void ClearOrders()
        {
            parent.ClearOrders();
        }

        public IEnumerable<T> Future<T>()
        {
            return parent.Future<T>();
        }

        public IFutureValue<T> FutureValue<T>()
        {
            return parent.FutureValue<T>();
        }

        public ICriteria GetCriteriaByAlias(string alias)
        {
            return parent.GetCriteriaByAlias(alias);
        }

        public ICriteria GetCriteriaByPath(string path)
        {
            return parent.GetCriteriaByPath(path);
        }

        public System.Type GetRootEntityTypeIfAvailable()
        {
            return parent.GetRootEntityTypeIfAvailable();
        }

        public void List(IList results)
        {
             parent.List(results);
        }

        public IList<T> List<T>()
        {
            return parent.List<T>();
        }

		/**
		 * @return Returns an actual Criteria object, or null if none have been allocated.
		 */
        private ICriteria GetSomeSubcriteria()
        {
            foreach (ICriteria crit in shardToCriteriaMap.Values)
            {
                if (crit != null)
                {
                    return crit;
                }
            }
            return null;
        }

		/**
		 * @return Returns an actual Criteria object.  If no actual Criteria object
		 * has been allocated, allocate one and return it.
		 */
        private ICriteria GetOrEstablishSomeSubcriteria()
        {
            ICriteria crit = GetSomeSubcriteria();
            if (crit == null)
            {
                IShard shard = shards[0];
				// this should trigger the creation of all subcriteria for the parent
                shard.EstablishCriteria(parent);
            }
            return GetSomeSubcriteria();
        }

        public string Alias
        {
            get { return GetOrEstablishSomeSubcriteria().Alias; }
        }

        public ICriteria SetProjection(params IProjection[] projections)
        {
            foreach (IProjection projection in projections)
            {
                foreach (IShard shard in shards)
                {
                    if (shardToCriteriaMap[shard] != null)
                    {
                        shardToCriteriaMap[shard].SetProjection(projection);
                    }
                    else
                    {
                        ICriteriaEvent criteriaEvent = new SetProjectionEvent(projection);
                        shardToEventListMap[shard].Add(criteriaEvent);
                    }
                }
            }
            return this;

        }

        public ICriteria SetProjection(IProjection projection)
        {
            ICriteriaEvent criteriaEvent = new SetProjectionEvent(projection);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetProjection(projection);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria Add(ICriterion criterion)
        {
            ICriteriaEvent criteriaEvent = new AddCriterionEvent(criterion);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].Add(criterion);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria AddOrder(Order order)
        {
            ICriteriaEvent criteriaEvent = new AddOrderEvent(order);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].AddOrder(order);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }

            return this;
        }

        public ICriteria SetFetchMode(string associationPath, FetchMode fetchMode)
        {
            ICriteriaEvent criteriaEvent = new SetFetchModeEvent(associationPath, fetchMode);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetFetchMode(associationPath, fetchMode);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetLockMode(LockMode lockMode)
        {
            ICriteriaEvent criteriaEvent = new SetLockModeEvent(lockMode);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetLockMode(lockMode);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetLockMode(string alias, LockMode lockMode)
        {
            ICriteriaEvent criteriaEvent = new SetLockModeEvent(lockMode, alias);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetLockMode(alias, lockMode);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria CreateAlias(string associationPath, string alias)
        {
            ICriteriaEvent criteriaEvent = new CreateAliasEvent(associationPath, alias);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].CreateAlias(associationPath, alias);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType)
        {
            ICriteriaEvent criteriaEvent = new CreateAliasEvent(associationPath, alias, joinType);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].CreateAlias(associationPath, alias, joinType);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetResultTransformer(IResultTransformer resultTransformer)
        {
            ICriteriaEvent criteriaEvent = new SetResultTransformerEvent(resultTransformer);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetResultTransformer(resultTransformer);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

		/**
		 * TODO(maxr)
		 * This clearly isn't what people want.  We should be building an
		 * exit strategy that returns once we've accumulated maxResults
		 * across _all_ shards, not each shard.
		 */
        public ICriteria SetMaxResults(int maxResults)
        {
            ICriteriaEvent criteriaEvent = new SetMaxResultsEvent(maxResults);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetMaxResults(maxResults);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }


        public ICriteria SetFirstResult(int firstResult)
        {
            ICriteriaEvent criteriaEvent = new SetFirstResultEvent(firstResult);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetFirstResult(firstResult);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetFetchSize(int fetchSize)
        {
            ICriteriaEvent criteriaEvent = new SetFetchSizeEvent(fetchSize);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetFetchSize(fetchSize);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetTimeout(int timeout)
        {
            ICriteriaEvent criteriaEvent = new SetTimeoutEvent(timeout);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetTimeout(timeout);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetCacheable(bool cacheable)
        {
            ICriteriaEvent criteriaEvent = new SetCacheableEvent(cacheable);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetCacheable(cacheable);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetCacheRegion(string cacheRegion)
        {
            ICriteriaEvent criteriaEvent = new SetCacheRegionEvent(cacheRegion);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetCacheRegion(cacheRegion);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetComment(string comment)
        {
            ICriteriaEvent criteriaEvent = new SetCommentEvent(comment);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetComment(comment);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetFlushMode(FlushMode flushMode)
        {
            ICriteriaEvent criteriaEvent = new SetFlushModeEvent(flushMode);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetFlushMode(flushMode);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public ICriteria SetCacheMode(CacheMode cacheMode)
        {
            ICriteriaEvent criteriaEvent = new SetCacheModeEvent(cacheMode);
            foreach (IShard shard in shards)
            {
                if (shardToCriteriaMap[shard] != null)
                {
                    shardToCriteriaMap[shard].SetCacheMode(cacheMode);
                }
                else
                {
                    shardToEventListMap[shard].Add(criteriaEvent);
                }
            }
            return this;
        }

        public IList List()
        {
			// pass through to the parent
            return ParentCriteria.List();
        }


        //public IScrollableResults Scroll()
        //{
        //    return ParentCriteria.Scroll();
        //}

        //public IScrollableResults Scroll(ScrollMode scrollMode)
        //{
        //    return ParentCriteria.Scroll(scrollMode);
        //}

        public object UniqueResult()
        {
            return ParentCriteria.UniqueResult();
        }

        private ShardedSubcriteriaImpl CreateSubCriteria(ISubcriteriaFactory factory)
        {
			// first build our sharded subcrit
            var subCrit = new ShardedSubcriteriaImpl(shards, parent);
            foreach (IShard shard in shards)
            {
				// see if we already have a concreate Criteria object for each shard
                if (shardToCriteriaMap[shard] != null)
                {
					// we already have a concreate Criteria for this shard, so create
					// a subcrit for it using the provided factory
                    factory.CreateSubcriteria(this, shardToEventListMap[shard]);
                }
                else
                {
					// we do not yet have a concrete Criteria object for this shard
					// so register an event that will create a proper subcrit when we do
                    ICriteriaEvent subCriteriaEvent = new CreateSubcriteriaEvent(factory,
                                                                                         subCrit.SubcriteriaRegistrar(
                                                                                             shard), shardToCriteriaMap,
                                                                                         shardToEventListMap);
                    shardToEventListMap[shard].Add(subCriteriaEvent);
                }
            }
            return subCrit;
        }

        public ISubcriteriaRegistrar SubcriteriaRegistrar(IShard shard)
        {
            return new SubcriteriaRegistrar(shard);
        }

        public ICriteria CreateCriteria(string associationPath)
        {
            ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath);
            return CreateSubCriteria(factory);
        }

        public ICriteria CreateCriteria(string associationPath, JoinType joinType)
        {
            ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, joinType);
            return CreateSubCriteria(factory);
        }

        public ICriteria CreateCriteria(string associationPath, string alias)
        {
            ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, alias);
            return CreateSubCriteria(factory);
        }

        public ICriteria CreateCriteria(string associationpath, string alias, JoinType joinType)
        {
            ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationpath, alias, joinType);
            return CreateSubCriteria(factory);
        }

        public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType, ICriterion withClause)
        {
            throw new System.NotSupportedException();
        }

        public ICriteria CreateCriteria(string associationPath, string alias, JoinType joinType, ICriterion withClause)
        {
            throw new System.NotSupportedException();
        }

        public IShardedCriteria ParentCriteria
        {
            get { return this.parent; }

        }
        
        public IDictionary<IShard, ICriteria> ShardToCriteriaMap
        {
            get { return this.shardToCriteriaMap; }
        }

        public IDictionary<IShard,IList<ICriteriaEvent>> ShardToEventListMap
        {
            get { return this.shardToEventListMap; }
        }
    }
}
