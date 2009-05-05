//using System;
//using NHibernate.Engine;
//using NHibernate.Shards;
//using NHibernate.Shards.Criteria;
//using NHibernate.Shards.Strategy.Access;
//using NHibernate.Criterion;
//using System.Collections.Generic;
//using NHibernate.SqlCommand;

//namespace NHibernate.Shards.Criteria
//{
//    public class ShardedCriteriaImpl
//    {
          
//      // unique id for this ShardedCriteria
//      private readonly CriteriaId criteriaId;

//      // the shards we know about
//      private readonly IList<IShard> shards;

//      // a factory that knows how to create actual Criteria objects
//      private readonly ICriteriaFactory criteriaFactory;

//      // the shard access strategy we use when we execute the Criteria
//      // across multiple shards
//      private readonly IShardAccessStrategy shardAccessStrategy;

//      // the criteria collector we use to process the results of executing
//      // the Criteria across multiple shards
//      readonly ExitOperationsCriteriaCollector criteriaCollector;

//      // the last value with which setFirstResult was called
//      private int firstResult;

//      // the last value with which maxResults was called
//      private int maxResults;

//      /**
//       * Construct a ShardedCriteriaImpl
//       *
//       * @param criteriaId unique id for this ShardedCriteria
//       * @param shards the shards that this ShardedCriteria is aware of
//       * @param criteriaFactory factory that knows how to create concrete {@link Criteria} objects
//       * @param shardAccessStrategy the access strategy we use when we execute this
//       * ShardedCriteria across multiple shards.
//       */
//      public ShardedCriteriaImpl(
//          CriteriaId criteriaId,
//          IList<IShard> shards,
//          ICriteriaFactory criteriaFactory,
//          IShardAccessStrategy shardAccessStrategy) {
//        this.criteriaId = criteriaId;
//        this.shards = shards;
//        this.criteriaFactory = criteriaFactory;
//        this.shardAccessStrategy = shardAccessStrategy;
//        criteriaCollector = new ExitOperationsCriteriaCollector();
//        criteriaCollector.setSessionFactory(shards.get(0).getSessionFactoryImplementor());
//      }

//      CriteriaId CriteriaId {get;}

//      ICriteriaFactory CriteriaFactory {get;}
      
//      /**
//       * @return any Criteria, or null if we don't have one
//       */
//      private /*@Nullable*/ ICriteria GetSomeCriteria() {
//        foreach (IShard shard in shards) {
//          ICriteria crit = shard.GetCriteriaById(criteriaId);
//          if (crit != null) {
//            return crit;
//          }
//        }
//        return null;
//      }

//      /**
//       * @return any Criteria.  If no Criteria has been established we establish
//       * one and return it.
//       */
//      private ICriteria GetOrEstablishSomeCriteria() {
//        ICriteria crit = GetSomeCriteria();
//        if(crit == null) {
//          IShard shard = shards.get(0);
//          crit = shard.EstablishCriteria(this);
//        }
//        return crit;
//      }

//      public string GetAlias() {
//        return GetOrEstablishSomeCriteria().GetAlias();
//      }

//      public ICriteria SetProjection(IProjection projection) {
//          if (projection != null)
//          {
//              criteriaCollector.AddProjection(projection);
//              if(projection is AvgProjection) {
//                  SetAvgProjection(projection);
//              }
//          }
//          // TODO - handle ProjectionList
//        return this as ICriteria;
//      }

//      private void SetAvgProjection(IProjection projection) {
//        // We need to modify the query to pull back not just the average but also
//        // the count.  We'll do this by creating a ProjectionList with both the
//        // average and the row count.
//        ProjectionList projectionList = Projections.ProjectionList();
//        projectionList.Add(projection);
//        projectionList.Add(Projections.RowCount());
//        ICriteriaEvent @event = new SetProjectionEvent(projectionList);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetProjection(projectionList);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//      }  
      

//      public ICriteria Add(ICriterion criterion) {
//        ICriteriaEvent @event = new AddCriterionEvent(criterion);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).Add(criterion);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria AddOrder(Order order) {
//        // Order applies to top-level object so we pass a null association path
//        criteriaCollector.AddOrder(null, order);
//        ICriteriaEvent @event = new AddOrderEvent(order);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).AddOrder(order);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria SetFetchMode(string associationPath, FetchMode mode)
//      {
//          ICriteriaEvent @event = new SetFetchModeEvent(associationPath, mode);
//          foreach (IShard shard in shards) {
//              if (shard.GetCriteriaById(criteriaId) != null) {
//                  shard.GetCriteriaById(criteriaId).SetFetchMode(associationPath, mode);
//              } else {
//                  shard.AddCriteriaEvent(criteriaId, @event);
//              }
//          }
//          return this as ICriteria;
//      }

//      public ICriteria SetLockMode(LockMode lockMode) {
//        ICriteriaEvent @event = new SetLockModeEvent(lockMode);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetLockMode(lockMode);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria SetLockMode(string alias, LockMode lockMode) {
//        ICriteriaEvent @event = new SetLockModeEvent(lockMode, alias);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetLockMode(lockMode);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria CreateAlias(string associationPath, string alias)
//          /*throws HibernateException*/ {
//        var @event = new CreateAliasEvent(associationPath, alias) as ICriteriaEvent;
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).CreateAlias(associationPath, alias);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria CreateAlias(string associationPath, string alias, JoinType joinType)
//        {
//        var @event = new CreateAliasEvent(associationPath, alias, joinType) as ICriteriaEvent;
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId)
//                .CreateAlias(associationPath, alias, joinType);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      private static readonly Iterable<ICriteriaEvent> NoCriteriaEvents =
//          Collections.UnmodifiableList(new ArrayList<ICriteriaEvent>());

//      /**
//       * Creating sharded subcriteria is tricky.  We need to give the client a
//       * reference to a ShardedSubcriteriaImpl (which to the client just looks like
//       * a Criteria object).  Then, for each shard where the Criteria has already been
//       * established we need to create the actual subcriteria, and for each shard
//       * where the Criteria has not yet been established we need to register an
//       * event that will create the Subcriteria when the Criteria is established.
//       *
//       * @param factory the factory to use to create the subcriteria
//       * @param associationPath the association path to the property on which we're
//       * creating a subcriteria
//       *
//       * @return a new ShardedSubcriteriaImpl
//       */
//      //private ShardedSubcriteriaImpl CreateSubcriteria(
//      //    ISubcriteriaFactory factory, string associationPath) {

//        ShardedSubcriteriaImpl subcrit =
//            new ShardedSubcriteriaImpl(shards, this, criteriaCollector, associationPath);

//        foreach (IShard shard in shards) {
//          ICriteria crit = shard.GetCriteriaById(criteriaId);
//          if(crit != null) {
//            factory.CreateSubcriteria(crit, NoCriteriaEvents);
//          } else {
//            CreateSubcriteriaEvent @event =
//                new CreateSubcriteriaEvent(factory, subcrit.GetSubcriteriaRegistrar(shard));
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return subcrit;
//      }

//      public ICriteria CreateCriteria(string associationPath)
//        /*throws HibernateException*/ {
//        ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath);
//        return CreateSubcriteria(factory, associationPath);
//      }

//      public ICriteria CreateCriteria(string associationPath, int joinType)
//         /*throws HibernateException*/ {
//        ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, joinType);
//        return createSubcriteria(factory, associationPath);
//      }

//      public ICriteria CreateCriteria(string associationPath, String alias)
//       /*throws HibernateException*/ {
//        ISubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, alias);
//        return createSubcriteria(factory, associationPath);
//      }

//      public ICriteria CreateCriteria(string associationPath, String alias,
//          int joinType) /*throws HibernateException*/ {
//        SubcriteriaFactory factory = new SubcriteriaFactoryImpl(associationPath, alias, joinType);
//        return createSubcriteria(factory, associationPath);
//      }

//      public ICriteria SetResultTransformer(ResultTransformer resultTransformer) {
//        ICriteriaEvent @event = new SetResultTransformerEvent(resultTransformer);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId)
//                .SetResultTransformer(resultTransformer);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this;
//      }

//      /*
//          A description of the trickyness that goes on with first result and
//          max result:
//          You can safely apply the maxResult on each individual shard so long as there
//          is no firstResult specified.  If firstResult is specified you can't
//          safely apply it on each shard but you can set maxResult to be the existing
//          value of maxResult + firstResult.
//       */

//      public ICriteria SetMaxResults(int maxResults) {
//        // the criteriaCollector will use the maxResult value that was passed in
//        criteriaCollector.SetMaxResults(maxResults);
//        this.maxResults = maxResults;
//        int adjustedMaxResults = maxResults + firstResult;
//        // the query executed against each shard will use maxResult + firstResult
//        SetMaxResultsEvent @event = new SetMaxResultsEvent(adjustedMaxResults);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetMaxResults(adjustedMaxResults);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria SetFirstResult(int firstResult) {
//        criteriaCollector.SetFirstResult(firstResult);
//        this.firstResult = firstResult;
//        // firstResult cannot be safely applied to the Criteria that will be
//        // executed against the Shard.  If a maxResult has been set we need to adjust
//        // that to take the firstResult into account.  Just calling setMaxResults
//        // will take care of this for us.
//        if(maxResults != null) {
//          setMaxResults(maxResults);
//        }
//        return this as ICriteria;
//      }

//      public ICriteria SetFetchSize(int fetchSize) {
//        ICriteriaEvent @event = new SetFetchSizeEvent(fetchSize);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetFetchSize(fetchSize);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria setTimeout(int timeout) {
//        ICriteriaEvent @event = new SetTimeoutEvent(timeout);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetTimeout(timeout);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria SetCacheable(bool cacheable) {
//        ICriteriaEvent @event = new SetCacheableEvent(cacheable);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetCacheable(cacheable);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria SetCacheRegion(string cacheRegion) {
//        ICriteriaEvent @event = new SetCacheRegionEvent(cacheRegion);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetCacheRegion(cacheRegion);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria SetComment(String comment) {
//        ICriteriaEvent @event = new SetCommentEvent(comment);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetComment(comment);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria SetFlushMode(FlushMode flushMode) {
//        ICriteriaEvent @event = new SetFlushModeEvent(flushMode);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetFlushMode(flushMode);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      public ICriteria SetCacheMode(CacheMode cacheMode) {
//        ICriteriaEvent @event = new SetCacheModeEvent(cacheMode);
//        foreach (IShard shard in shards) {
//          if (shard.GetCriteriaById(criteriaId) != null) {
//            shard.GetCriteriaById(criteriaId).SetCacheMode(cacheMode);
//          } else {
//            shard.AddCriteriaEvent(criteriaId, @event);
//          }
//        }
//        return this as ICriteria;
//      }

//      /**
//       * Unsupported.  This is a scope decision, not a technical decision.
//       */
//      public ScrollableResults scroll() throws HibernateException {
//        throw new UnsupportedOperationException();
//      }

//      /**
//       * Unsupported.  This is a scope decision, not a technical decision.
//       */
//      public ScrollableResults scroll(ScrollMode scrollMode)
//          throws HibernateException {
//        throw new UnsupportedOperationException();
//      }

//      public IList list() throws HibernateException {

//        // build a shard operation and apply it across all shards
//        ShardOperation<List<Object>> shardOp = new ShardOperation<IList<Object>>() {
//          public List<Object> execute(Shard shard) {
//            shard.establishCriteria(ShardedCriteriaImpl.this);
//            return shard.list(criteriaId);
//          }

//          public String getOperationName() {
//            return "list()";
//          }
//        };
//        /**
//         * We don't support shard selection for criteria queries.  If you want
//         * custom shards, create a ShardedSession with only the shards you want.
//         * We're going to concatenate all our results and then use our
//         * criteria collector to do post processing.
//         */
//        return
//            shardAccessStrategy.apply(
//                shards,
//                shardOp,
//                new ConcatenateListsExitStrategy(),
//                criteriaCollector);
//      }

//      public Object uniqueResult() throws HibernateException {
//        // build a shard operation and apply it across all shards
//        ShardOperation<Object> shardOp = new ShardOperation<Object>() {
//          public Object execute(Shard shard) {
//            shard.establishCriteria(ShardedCriteriaImpl.this);
//            return shard.uniqueResult(criteriaId);
//          }

//          public String getOperationName() {
//            return "uniqueResult()";
//          }
//        };
//        /**
//         * We don't support shard selection for criteria queries.  If you want
//         * custom shards, create a ShardedSession with only the shards you want.
//         * We're going to return the first non-null result we get from a shard.
//         */
//        return
//            shardAccessStrategy.apply(
//                shards,
//                shardOp,
//                new FirstNonNullResultExitStrategy<Object>(),
//                criteriaCollector);
//      }

//        }
//}
