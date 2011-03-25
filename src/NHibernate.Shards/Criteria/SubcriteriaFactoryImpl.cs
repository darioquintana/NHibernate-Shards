using System.Collections.Generic;
using NHibernate.Shards.Session;
using NHibernate.SqlCommand;

namespace NHibernate.Shards.Criteria
{
	/**
	 * Concrete implementation of the {@link SubcriteriaFactory} interface.
	 * Used to lazily create {@link org.hibernate.impl.CriteriaImpl.Subcriteria}
	 * @see Criteria#createCriteria(String)
	 * @see Criteria#createCriteria(String, int)
	 * @see Criteria#createCriteria(String, String)
	 * @see Criteria#createCriteria(String, String, int)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
    public class SubcriteriaFactoryImpl:ISubcriteriaFactory
    {
        private enum MethodSig
        {
            Association,
            AssociationAndJoinType,
            AssociationAndAlias,
            AssociationAndAliasAndJoinType
        }

		// used to tell us which overload of createCriteria to invoke
        private readonly MethodSig methodSig;

		// the association we'll pass to createCriteria
        private readonly string association;

		// the join type we'll pass to createCriteria.  Can be null.
		private readonly JoinType joinType;

		// the alias we'll pass to createCriteria.  Can be null.
        private readonly string alias;

		/**
		 * Construct a SubcriteriaFactoryImpl
		 *
		 * @param methodSig used to tell us which overload of createCriteria to invoke
		 * @param association the association we'll pass to createCriteria
		 * @param joinType the join type we'll pass to createCriteria.  Can be null.
		 * @param alias the alias we'll pass to createCriteria.  Can be null.
		 */
        private SubcriteriaFactoryImpl(MethodSig methodSig, string association, JoinType joinType, string alias)
        {
            this.methodSig = methodSig;
            this.association = association;
            this.joinType = joinType;
            this.alias = alias;
        }

		/**
		 * Construct a SubcriteriaFactoryImpl
		 *
		 * @param association the association we'll pass to createCriteria
		 */
        public SubcriteriaFactoryImpl(string association):this(MethodSig.Association,association,JoinType.None,null)
        {            
        }

		/**
		 * Construct a SubcriteriaFactoryImpl
		 *
		 * @param association the association we'll pass to createCriteria
		 * @param joinType the join type we'll pass to createCriteria
		 */
        public SubcriteriaFactoryImpl(string association, JoinType joinType)
            : this(MethodSig.AssociationAndJoinType, association, joinType, null)
        {
            
        }

		/**
		 * Construct a SubcriteriaFactoryImpl
		 *
		 * @param association the association we'll pass to createCriteria
		 * @param alias the alias we'll pass to createCriteria
		 */
        public SubcriteriaFactoryImpl(string association,string alias):this(MethodSig.AssociationAndAlias,association,JoinType.None,alias)
        {
            
        }

		/**
		 * Construct a SubcriteriaFactoryImpl
		 *
		 * @param association the association we'll pass to createCriteria
		 * @param alias the alias we'll pass to createCriteria
		 * @param joinType the join type we'll pass to createCriteria
		 */
        public SubcriteriaFactoryImpl(string association, string alias, JoinType joinType):this(MethodSig.AssociationAndAliasAndJoinType,association,joinType,alias)
        {
            
        }

        public ICriteria CreateSubcriteria(ICriteria parent, IEnumerable<ICriteriaEvent> criteriaEvents)
        {
			// call the right overload to actually create the Criteria
            ICriteria crit;
            switch(methodSig)
            {
                case MethodSig.Association:
                    crit = parent.CreateCriteria(association);
                    break;
                case MethodSig.AssociationAndJoinType:
                    crit = parent.CreateCriteria(association, joinType);
                    break;
                case MethodSig.AssociationAndAlias:
                    crit = parent.CreateCriteria(association, alias);
                    break;
                case MethodSig.AssociationAndAliasAndJoinType:
                    crit = parent.CreateCriteria(association, alias, joinType);
                    break;
                default:
                    throw new ShardedSessionException("Unknown constructor type for subcriteria creation: " + methodSig);
            }
			// apply the events
            foreach(ICriteriaEvent criteriaEvent in criteriaEvents)
            {
                criteriaEvent.OnEvent(crit);
            }
            return crit;
        }

    }
}
