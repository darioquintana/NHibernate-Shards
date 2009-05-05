//using System;
//using NHibernate.Shards.Session;
//using NHibernate.SqlCommand;

//namespace NHibernate.Shards.Criteria
//{
//    public class CreateAliasEvent
//    {
//          private enum MethodSig {
//            AssocPathAndAlias,
//            AssocPathAndAliasAndJoinType
//          }

//          // the signature of the createAlias method we're going to invoke when
//          // the event fires
//          private readonly MethodSig methodSig;

//          // the association path
//          private readonly String associationPath;

//          // the name of the alias we're creating
//          private readonly String alias;

//          // the join type - we look at method sig to see if we should use it
//          private readonly int joinType;

//          /**
//           * Construct a CreateAliasEvent
//           *
//           * @param methodSig the signature of the createAlias method we're going to invoke
//           * when the event fires
//           * @param associationPath the association path of the alias we're creating.
//           * @param alias the name of the alias we're creating.
//           * @param joinType the join type of the alias we're creating.  Can be null.
//           */
//          private CreateAliasEvent(
//              MethodSig methodSig,
//              String associationPath,
//              String alias,
//              /*@Nullable*/int joinType) {
//            this.methodSig = methodSig;
//            this.associationPath = associationPath;
//            this.alias = alias;
//            this.joinType = joinType;
//          }

//          /**
//           * Construct a CreateAliasEvent
//           *
//           * @param associationPath the association path of the alias we're creating.
//           * @param alias the name of the alias we're creating.
//           */
//          public CreateAliasEvent(String associationPath, String alias) {
//            this(MethodSig.AssocPathAndAlias, associationPath, alias, null);
//          }

//          /**
//           * Construct a CreateAliasEvent
//           *
//           * @param associationPath the association path of the alias we're creating.
//           * @param alias the name of the alias we're creating.
//           * @param joinType the join type of the alias we're creating.
//           */
//          public CreateAliasEvent(String associationPath, String alias, JoinType joinType) {
//            this(MethodSig.AssocPathAndAliasAndJoinType, associationPath, alias,
//                joinType);
//          }

//          public void OnEvent(ICriteria crit) {
//            switch (methodSig) {
//              case ASSOC_PATH_AND_ALIAS:
//                crit.CreateAlias(associationPath, alias);
//                break;
//              case ASSOC_PATH_AND_ALIAS_AND_JOIN_TYPE:
//                crit.CreateAlias(associationPath, alias, joinType);
//                break;
//              default:
//                throw new ShardedSessionException(
//                    "Unknown ctor type in CreateAliasEvent: " + methodSig);
//            }
//          }

//            }
//}
