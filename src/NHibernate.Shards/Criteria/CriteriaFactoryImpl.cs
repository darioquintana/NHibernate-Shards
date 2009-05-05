//using System;
//using NHibernate.Shards.Session;

//namespace NHibernate.Shards.Criteria
//{
//    public class CriteriaFactoryImpl : ICriteriaFactory
//    {
      
//        private enum MethodSig {
//            CLASS,
//            CLASS_AND_ALIAS,
//            ENTITY,
//            ENTITY_AND_ALIAS
//        };

//          // the signature of the CreateCriteria method we're going to invoke when
//          // the event fires
//          private readonly MethodSig MethodSig;

//          // the Class we'll use when we create the Criteria.  We look at methodSig to
//          // see if we should use it.
//          private readonly class PersistentClass{};

//          // the alias we'll use when we create the Criteria.  We look at methodSig to
//          // see if we should use it.
//          private readonly string alias;

//          // the entity name we'll use when we create the Criteria.  We look at methodSig to
//          // see if we should use it.
//          private readonly string entityName;

//          /**
//           * Create a CriteriaFactoryImpl
//           *
//           * @param methodSig the signature of the CreateCriteria method we'll invoke
//           * when the event fires.
//           * @param persistentClass the {@link Class} of the {@link Criteria} we're creating.
//           * Can be null.
//           * @param alias the alias of the {@link Criteria} we're creating.  Can be null.
//           * @param entityName the entity name of the {@link} Criteria we're creating.
//           * Can be null.
//           */
//          private CriteriaFactoryImpl(
//              MethodSig methodSig,
//              /*@Nullable*/ class persistentClass,
//              /*@Nullable*/ string alias,
//              /*@Nullable*/ string entityName) {
//            this.MethodSig = methodSig;
//            this.persistentClass = persistentClass;
//            this.alias = alias;
//            this.entityName = entityName;
//          }

//          /**
//           * Create a CriteriaFactoryImpl.
//           *
//           * @param persistentClass the {@link Class} of the {@link Criteria} we're creating.
//           */
//          public CriteriaFactoryImpl(Class persistentClass) {
//            this(MethodSig.CLASS, persistentClass, null, null);
//          }

//          /**
//           * Create a CriteriaFactoryImpl.
//           *
//           * @param persistentClass the {@link Class} of the {@link Criteria} we're creating.
//           * @param alias the alias of the {@link Criteria} we're creating.
//           */
//          public CriteriaFactoryImpl(Class persistentClass, string alias) {
//            this(MethodSig.CLASS_AND_ALIAS, persistentClass, alias, null);
//          }

//          /**
//           * Create a CriteriaFactoryImpl.
//           *
//           * @param entityName the entity name of the {@link Criteria} we're creating.
//           */
//          public CriteriaFactoryImpl(String entityName) {
//            this(MethodSig.ENTITY, null, null, entityName);
//          }

//          /**
//           * Create a CriteriaFactoryImpl.
//           *
//           * @param entityName the entity name of the {@link Criteria} we're creating.
//           * @param alias the alias of the {@link Criteria} we're creating.
//           */
//          public CriteriaFactoryImpl(String entityName, String alias) {
//            this(MethodSig.ENTITY_AND_ALIAS, null, alias, entityName);
//          }

//          public ICriteria CreateCriteria(ISession session) {
//            switch (methodSig) {
//              case CLASS:
//                return session.createCriteria(PersistentClass);
//              case CLASS_AND_ALIAS:
//                return session.createCriteria(PersistentClass, alias);
//              case ENTITY:
//                return session.createCriteria(entityName);
//              case ENTITY_AND_ALIAS:
//                return session.createCriteria(entityName, alias);
//              default:
//                throw new ShardedSessionException(
//                    "Unknown constructor type for criteria creation: " + methodSig);
//            }
//          }
//        }

//            }
//}
