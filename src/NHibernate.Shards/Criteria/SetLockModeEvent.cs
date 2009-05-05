//using System;
//using NHibernate.Shards.Session;

//namespace NHibernate.Shards.Criteria
//{
//    public class SetLockModeEvent : ICriteriaEvent
//    {
//          private enum MethodSig {
//            LockMode,
//            LockModeAndAlias
//            }

//          // tells us which overload of setLockMode to use
//          private readonly MethodSig methodSig;

//          // the LockMode we'll set on the Criteria when the event fires
//          private readonly LockMode lockMode;

//          // the alias for which we'll set the lock mode on the Criteria when the event
//          // fires.  Can be null
//          private readonly string alias;

//          /**
//           * Construct a SetLockModeEvent
//           *
//           * @param methodSig tells us which overload of setLockMode to use
//           * @param lockMode the lock mode we'll set when the event fires
//           * @param alias the alias for which we'll set the lcok mode when the event
//           * fires.  Can be null.
//           */
//          private SetLockModeEvent(
//              MethodSig methodSig,
//              LockMode lockMode,
//              /*@Nullable*/ string alias) {
//            this.methodSig = methodSig;
//            this.lockMode = lockMode;
//            this.alias = alias;
//          }

//          /**
//           * Construct a SetLockModeEvent
//           *
//           * @param lockMode the lock mode we'll set when the event fires
//           */
//          public SetLockModeEvent(LockMode lockMode) {
//            this(MethodSig.LockMode, lockMode, null);
//          }

//          /**
//           * Construct a SetLockModeEvent
//           *
//           * @param lockMode the lock mode we'll set when the event fires
//           * @param alias the alias for which we'll set the lock mode
//           * when the event fires
//           */
//          public SetLockModeEvent(LockMode lockMode, String alias) {
//            this(MethodSig.LockModeAndAlias, lockMode, alias);
//          }

//          public void OnEvent(ICriteria crit) {
//            switch (methodSig) {
//              case LockMode:
//                crit.SetLockMode(lockMode);
//                break;
//              case LockModeAndAlias:
//                crit.SetLockMode(alias, lockMode);
//                break;
//              default:
//                throw new ShardedSessionException(
//                    "Unknown constructor type for SetLockModeEvent: " + methodSig);
//            }
//          }

//            }
//}



            