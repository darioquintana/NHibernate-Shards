//using System.Collections.Generic;
//using NHibernate.Shards.Session;

//namespace NHibernate.Shards.Criteria
//{
//    public class SubcriteriaFactoryImpl : ISubCriteriaFactory
//    {
//          private enum MethodSig {
//            Association,
//            AssociationAndJoinType,
//            AssociationAndAlias,
//            AssociationAndAliasAndJoinType
//          }

//          // used to tell us which overload of CreateCriteria to invoke
//          private readonly MethodSig methodSig;

//          // the association we'll pass to CreateCriteria
//          private readonly string association;

//          // the join type we'll pass to CreateCriteria.  Can be null.
//          private readonly int joinType;

//          // the alias we'll pass to CreateCriteria.  Can be null.
//          private readonly string alias;

//          /**
//           * Construct a SubcriteriaFactoryImpl
//           *
//           * @param methodSig used to tell us which overload of CreateCriteria to invoke
//           * @param association the association we'll pass to CreateCriteria
//           * @param joinType the join type we'll pass to CreateCriteria.  Can be null.
//           * @param alias the alias we'll pass to CreateCriteria.  Can be null.
//           */
//          private SubcriteriaFactoryImpl(
//              MethodSig methodSig,
//              string association,
//              /*@Nullable*/ int joinType,
//              /*@Nullable*/ string alias) {
//            this.methodSig = methodSig;
//            this.association = association;
//            this.joinType = joinType;
//            this.alias = alias;
//          }

//          /**
//           * Construct a SubcriteriaFactoryImpl
//           *
//           * @param association the association we'll pass to CreateCriteria
//           */
//          public SubcriteriaFactoryImpl(string association) {
//            this(MethodSig.Association, association, 0, null);
//          }

//          /**
//           * Construct a SubcriteriaFactoryImpl
//           *
//           * @param association the association we'll pass to CreateCriteria
//           * @param joinType the join type we'll pass to CreateCriteria
//           */
//          public SubcriteriaFactoryImpl(string association, int joinType) {
//            this(MethodSig.AssociationAndJoinType, association, joinType, null);
//          }

//          /**
//           * Construct a SubcriteriaFactoryImpl
//           *
//           * @param association the association we'll pass to CreateCriteria
//           * @param alias the alias we'll pass to CreateCriteria
//           */
//          public SubcriteriaFactoryImpl(string association, string alias) {
//            this(MethodSig.AssociationAndAlias, association, 0, alias);
//          }

//          /**
//           * Construct a SubcriteriaFactoryImpl
//           *
//           * @param association the association we'll pass to CreateCriteria
//           * @param alias the alias we'll pass to CreateCriteria
//           * @param joinType the join type we'll pass to CreateCriteria
//           */
//          public SubcriteriaFactoryImpl(string association, string alias, int joinType) {
//            this(MethodSig.AssociationAndAliasAndJoinType, association, joinType, alias);
//          }

//          public ICriteria CreateSubcriteria(ICriteria parent, IList<ICriteriaEvent> events)
//          {
//            // call the right overload to actually create the Criteria
//            ICriteria crit;
//            switch (methodSig) {
//              case Association:
//                crit = parent.CreateCriteria(association);
//                break;
//              case AssociationAndJoinType:
//                crit = parent.CreateCriteria(association, joinType);
//                break;
//              case AssociationAndAlias:
//                crit = parent.CreateCriteria(association, alias);
//                break;
//              case AssociationAndAliasAndJoinType:
//                crit = parent.CreateCriteria(association, alias, joinType);
//                break;
//              default:
//                throw new ShardedSessionException(
//                    "Unknown constructor type for subcriteria creation: " + methodSig);
//            }
//            // apply the events
//            foreach(ICriteriaEvent @event in events) {
//              @event.OnEvent(crit);
//            }
//            return crit;
//          }
//    }
//}
