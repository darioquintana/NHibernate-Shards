//using System;
//using System.Collections;
//using System.Collections.Generic;
//using NHibernate.Criterion;
//using NHibernate.Shards.Util;
//using NHibernate.SqlCommand;
//using NHibernate.Transform;

//namespace NHibernate.Shards.Criteria
//{
//    class ShardedSubcriteriaImpl : IShardedSubcriteria
//    {
//        // all shards that we're aware of
//          readonly IList<IShard> shards;

//          // our parent. As with CriteriaImpl, we pass-through certain operations
//          // to our parent
//          readonly IShardedCriteria parent;

//          // maps shards to actual Criteria objects
//          private readonly Dictionary<IShard, ICriteria> shardToCriteriaMap = Maps.newHashMap();

//          // maps shards to lists of criteria events that need to be applied
//          // when the actual Criteria objects are established
//          private readonly Dictionary<IShard, IList<ICriteriaEvent>> shardToEventListMap = Maps.newHashMap();

//          private readonly ExitOperationsCriteriaCollector criteriaCollector;

//          private readonly String associationPath;

//          /**
//           * Construct a ShardedSubcriteriaImpl
//           *
//           * @param shards the shards that we're aware of
//           * @param parent our parent
//           * @param criteriaCollector the collector for extit operations
//           * @param associationPath the association path for the subcriteria
//           */
//          public ShardedSubcriteriaImpl(List<IShard> shards, IShardedCriteria parent,
//              ExitOperationsCriteriaCollector criteriaCollector, String associationPath) {
//            Preconditions.CheckNotNull(shards);
//            Preconditions.CheckNotNull(parent);
//            Preconditions.CheckArgument(!shards.IsEmpty());
//            Preconditions.CheckNotNull(criteriaCollector);
//            Preconditions.CheckNotNull(associationPath);
//            this.shards = shards;
//            this.parent = parent;
//            this.criteriaCollector = criteriaCollector;
//            this.associationPath = associationPath;
//            // let's set up our maps
//            foreach(IShard shard in shards) {
//              shardToCriteriaMap.put(shard, null);
//              shardToEventListMap.put(shard, Lists.<ICriteriaEvent>newArrayList());
//            }
//          }

//          /**
//           * @return Returns an actual Criteria object, or null if none have been allocated.
//           */
//          private /*@Nullable*/ ICriteria GetSomeSubcriteria() {
//            foreach (ICriteria crit in shardToCriteriaMap.Values()) {
//              if (crit != null) {
//                return crit;
//              }
//            }
//            return null;
//          }

//          /**
//           * @return Returns an actual Criteria object.  If no actual Criteria object
//           * has been allocated, allocate one and return it.
//           */
//          private ICriteria GetOrEstablishSomeSubcriteria() {
//            ICriteria crit = GetSomeSubcriteria();
//            if(crit == null) {
//              IShard shard = shards.get(0);
//              // this should trigger the creation of all subcriteria for the parent
//              shard.EstablishCriteria(parent);
//            }
//            return GetSomeSubcriteria();
//          }

//          public string GetAlias() {
//            return GetOrEstablishSomeSubcriteria().getAlias();
//          }

//          public ICriteria SetProjection(IProjection projection) {
//            ICriteriaEvent @event = new SetProjectionEvent(projection);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setProjection(projection);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria Add(ICriterion criterion) {
//            ICriteriaEvent @event = new AddCriterionEvent(criterion);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).add(criterion);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria AddOrder(Order order) {
//            criteriaCollector.AddOrder(associationPath, order);
//            ICriteriaEvent @event = new AddOrderEvent(order);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).addOrder(order);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetFetchMode(string associationPath, FetchMode mode)
//            {
//            ICriteriaEvent @event = new SetFetchModeEvent(associationPath, mode);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setFetchMode(associationPath, mode);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetLockMode(LockMode lockMode) {
//            ICriteriaEvent @event = new SetLockModeEvent(lockMode);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setLockMode(lockMode);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetLockMode(String alias, LockMode lockMode) {
//            ICriteriaEvent @event = new SetLockModeEvent(lockMode, alias);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setLockMode(alias, lockMode);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria CreateAlias(String associationPath, String alias)
//            {
//            var @event = new CreateAliasEvent(associationPath, alias) as ICriteriaEvent;
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).createAlias(associationPath, alias);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//        public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType)
//        {
//            throw new NotImplementedException();
//        }

//        public ICriteria CreateCriteria(string associationPath)
//        {
//            throw new NotImplementedException();
//        }

//        public ICriteria CreateCriteria(string associationPath, JoinType joinType)
//        {
//            throw new NotImplementedException();
//        }

//        public ICriteria CreateCriteria(string associationPath, string alias)
//        {
//            throw new NotImplementedException();
//        }

//        public ICriteria CreateCriteria(string associationPath, string alias, JoinType joinType)
//        {
//            throw new NotImplementedException();
//        }

//        public ICriteria CreateAlias(String associationPath, String alias, int joinType)
//            {
//            ICriteriaEvent @event = new CreateAliasEvent(associationPath, alias, joinType);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).createAlias(associationPath, alias, joinType);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetResultTransformer(IResultTransformer resultTransformer) {
//            ICriteriaEvent @event = new SetResultTransformerEvent(resultTransformer);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setResultTransformer(resultTransformer);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetMaxResults(int maxResults) {
//            parent.SetMaxResults(maxResults);
//            return this;
//          }

//          public ICriteria SetFirstResult(int firstResult) {
//            parent.SetFirstResult(firstResult);
//            return this;
//          }

//          public ICriteria SetFetchSize(int fetchSize) {
//            ICriteriaEvent @event = new SetFetchSizeEvent(fetchSize);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setFetchSize(fetchSize);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetTimeout(int timeout) {
//            ICriteriaEvent @event = new SetTimeoutEvent(timeout);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setTimeout(timeout);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetCacheable(bool cacheable) {
//            ICriteriaEvent @event = new SetCacheableEvent(cacheable);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setCacheable(cacheable);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetCacheRegion(string cacheRegion) {
//            ICriteriaEvent @event = new SetCacheRegionEvent(cacheRegion);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setCacheRegion(cacheRegion);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetComment(String comment) {
//            ICriteriaEvent @event = new SetCommentEvent(comment);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setComment(comment);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetFlushMode(FlushMode flushMode) {
//            ICriteriaEvent @event = new SetFlushModeEvent(flushMode);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setFlushMode(flushMode);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//          public ICriteria SetCacheMode(CacheMode cacheMode) {
//            ICriteriaEvent @event = new SetCacheModeEvent(cacheMode);
//            foreach (IShard shard in shards) {
//              if (shardToCriteriaMap.get(shard) != null) {
//                shardToCriteriaMap.get(shard).setCacheMode(cacheMode);
//              } else {
//                shardToEventListMap.get(shard).add(@event);
//              }
//            }
//            return this;
//          }

//        public IList List()
//        {
//            throw new NotImplementedException();
//        }

//        public object UniqueResult()
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<T> Future<T>()
//        {
//            throw new NotImplementedException();
//        }

//        public IFutureValue<T> FutureValue<T>()
//        {
//            throw new NotImplementedException();
//        }

//        public void List(IList results)
//        {
//            throw new NotImplementedException();
//        }

//        public IList<T> List<T>()
//        {
//            throw new NotImplementedException();
//        }

//        public T UniqueResult<T>()
//        {
//            throw new NotImplementedException();
//        }

//        public void ClearOrders()
//        {
//            throw new NotImplementedException();
//        }

//        public ICriteria GetCriteriaByPath(string path)
//        {
//            throw new NotImplementedException();
//        }

//        public ICriteria GetCriteriaByAlias(string alias)
//        {
//            throw new NotImplementedException();
//        }

//        public System.Type GetRootEntityTypeIfAvailable()
//        {
//            throw new NotImplementedException();
//        }

//        public string Alias
//        {
//            get { throw new NotImplementedException(); }
//        }

//        //public list list()  {
//          //   pass through to the parent
//          //  return getparentcriteria().list();
//          //}

//          //public scrollableresults scroll() {
//          //   pass through to the parent
//          //  return getparentcriteria().scroll();
//          //}

//          //public scrollableresults scroll(scrollmode scrollmode)
//          //    throws hibernateexception {
//          //   pass through to the parent
//          //  return getparentcriteria().scroll(scrollmode);
//          //}

//          //public object uniqueresult() {
//          //   pass through to the parent
//          //  return getparentcriteria().uniqueresult();
//          //}

//          //private ShardedSubcriteriaImpl CreateSubcriteria(ISubcriteriaFactory factory, string newAssociationPath)
//          //{
//          //  string fullAssociationPath = associationPath + "." + newAssociationPath;
//          //  // first build our sharded subcrit
//          //  ShardedSubcriteriaImpl subcrit =
//          //      new ShardedSubcriteriaImpl(shards, parent, criteriaCollector, fullAssociationPath);
//          //  foreach (IShard shard in shards) {
//          //    // see if we already have a concreate Criteria object for each shard
//          //    if (shardToCriteriaMap.get(shard) != null) {
//          //      // we already have a concreate Criteria for this shard, so create
//          //      // a subcrit for it using the provided factory
//          //      factory.createSubcriteria(this, shardToEventListMap.get(shard));
//          //    } else {
//          //      // we do not yet have a concrete Criteria object for this shard
//          //      // so register an event that will create a proper subcrit when we do
//          //      CreateSubcriteriaEvent event = new CreateSubcriteriaEvent(factory, subcrit.getSubcriteriaRegistrar(shard));
//          //      shardToEventListMap.get(shard).add(event);
//          //    }
//          //  }
//          //  return subcrit;
//          //}

//          //ISubcriteriaRegistrar GetSubcriteriaRegistrar(readonly IShard shard) {
//          //  return new ISubcriteriaRegistrar() {

//          //    public void establishSubcriteria(Criteria parentCriteria, SubcriteriaFactory subcriteriaFactory) {
//          //      List<CriteriaEvent> criteriaEvents = shardToEventListMap.get(shard);
//          //      // create the subcrit with the proper list of events
//          //      Criteria newCrit = subcriteriaFactory.createSubcriteria(parentCriteria, criteriaEvents);
//          //      // clear the list of events
//          //      criteriaEvents.clear();
//          //      // add it to our map
//          //      shardToCriteriaMap.put(shard, newCrit);
//          //    }
//          //  };
//          //}

//          //public ICriteria CreateCriteria(string associationPath)
//          //{
//          //  ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath);
//          //  return createSubcriteria(factory, associationPath);
//          //}

//          //public ICriteria CreateCriteria(String associationPath, int joinType)
//          //{
//          //  ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, joinType);
//          //  return createSubcriteria(factory, associationPath);
//          //}

//          //public ICriteria CreateCriteria(String associationPath, String alias)
//          //{
//          //  ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, alias);
//          //  return createSubcriteria(factory, associationPath);
//          //}

//          //public ICriteria CreateCriteria(String associationPath, String alias,int joinType){
//          //  ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, alias, joinType);
//          //  return CreateSubcriteria(factory, associationPath);
//          //}

//          public IShardedCriteria GetParentCriteria() {
//            return parent;
//          }

//          Dictionary<IShard, ICriteria> GetShardToCriteriaMap() {
//            return shardToCriteriaMap;
//          }

//          //Map<IShard, List<ICriteriaEvent>> getShardToEventListMap() {
//          //  return shardToEventListMap;
//          //}

//          //interface ISubcriteriaRegistrar {

//          //  void establishSubcriteria(ICriteria parentCriteria, ISubcriteriaFactory subcriteriaFactory);

//          //}

//        public object Clone()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
