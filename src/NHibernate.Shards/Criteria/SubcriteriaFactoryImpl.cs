using NHibernate.Shards.Session;

namespace NHibernate.Shards.Criteria
{
    public class SubcriteriaFactoryImpl : ISubCriteriaFactory
    {
          private enum MethodSig {
            ASSOCIATION,
            ASSOCIATION_AND_JOIN_TYPE,
            ASSOCIATION_AND_ALIAS,
            ASSOCIATION_AND_ALIAS_AND_JOIN_TYPE
          }

          // used to tell us which overload of CreateCriteria to invoke
          private readonly MethodSig methodSig;

          // the association we'll pass to CreateCriteria
          private readonly string association;

          // the join type we'll pass to CreateCriteria.  Can be null.
          private readonly int joinType;

          // the alias we'll pass to CreateCriteria.  Can be null.
          private readonly string alias;

          /**
           * Construct a SubcriteriaFactoryImpl
           *
           * @param methodSig used to tell us which overload of CreateCriteria to invoke
           * @param association the association we'll pass to CreateCriteria
           * @param joinType the join type we'll pass to CreateCriteria.  Can be null.
           * @param alias the alias we'll pass to CreateCriteria.  Can be null.
           */
          private SubcriteriaFactoryImpl(
              MethodSig methodSig,
              string association,
              /*@Nullable*/ int joinType,
              /*@Nullable*/ string alias) {
            this.methodSig = methodSig;
            this.association = association;
            this.joinType = joinType;
            this.alias = alias;
          }

          /**
           * Construct a SubcriteriaFactoryImpl
           *
           * @param association the association we'll pass to CreateCriteria
           */
          public SubcriteriaFactoryImpl(string association) {
            this(MethodSig.ASSOCIATION, association, 0, null);
          }

          /**
           * Construct a SubcriteriaFactoryImpl
           *
           * @param association the association we'll pass to CreateCriteria
           * @param joinType the join type we'll pass to CreateCriteria
           */
          public SubcriteriaFactoryImpl(string association, int joinType) {
            this(MethodSig.ASSOCIATION_AND_JOIN_TYPE, association, joinType, null);
          }

          /**
           * Construct a SubcriteriaFactoryImpl
           *
           * @param association the association we'll pass to CreateCriteria
           * @param alias the alias we'll pass to CreateCriteria
           */
          public SubcriteriaFactoryImpl(string association, string alias) {
            this(MethodSig.ASSOCIATION_AND_ALIAS, association, 0, alias);
          }

          /**
           * Construct a SubcriteriaFactoryImpl
           *
           * @param association the association we'll pass to CreateCriteria
           * @param alias the alias we'll pass to CreateCriteria
           * @param joinType the join type we'll pass to CreateCriteria
           */
          public SubcriteriaFactoryImpl(string association, string alias, int joinType) {
            this(MethodSig.ASSOCIATION_AND_ALIAS_AND_JOIN_TYPE, association, joinType, alias);
          }

          public ICriteria createSubcriteria(ICriteria parent, Iterable<CriteriaEvent> events) {
            // call the right overload to actually create the Criteria
            ICriteria crit;
            switch (methodSig) {
              case ASSOCIATION:
                crit = parent.createCriteria(association);
                break;
              case ASSOCIATION_AND_JOIN_TYPE:
                crit = parent.createCriteria(association, joinType);
                break;
              case ASSOCIATION_AND_ALIAS:
                crit = parent.createCriteria(association, alias);
                break;
              case ASSOCIATION_AND_ALIAS_AND_JOIN_TYPE:
                crit = parent.createCriteria(association, alias, joinType);
                break;
              default:
                throw new ShardedSessionException(
                    "Unknown constructor type for subcriteria creation: " + methodSig);
            }
            // apply the events
            for(ICriteriaEvent event : events) {
              event.OnEvent(crit);
            }
            return crit;
          }

            }
}
