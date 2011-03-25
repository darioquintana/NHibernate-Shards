using NHibernate.Shards.Session;

namespace NHibernate.Shards.Criteria
{
	/**
	 * Implementation of the {@link CriteriaFactory} interface.
	 * @see Session#createCriteria(Class)
	 * @see Session#createCriteria(Class, String)
	 * @see Session#createCriteria(String)
	 * @see Session#createCriteria(String, String)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
	public class CriteriaFactoryImpl : ICriteriaFactory
	{
		private enum MethodSig
		{
			Class,
			ClassAndAlias,
			Entity,
			EntityAndAlias
		}

		// the signature of the createCriteria method we're going to invoke when
		// the event fires
	    private readonly MethodSig methodSig;

		// the Class we'll use when we create the Criteria.  We look at methodSig to
		// see if we should use it.
	    private readonly System.Type persistentClass;

		// the alias we'll use when we create the Criteria.  We look at methodSig to
		// see if we should use it.
	    private readonly string alias;

		// the entity name we'll use when we create the Criteria.  We look at methodSig to
		// see if we should use it.
	    private readonly string entityName;

		/**
		 * Create a CriteriaFactoryImpl
		 *
		 * @param methodSig the signature of the createCriteria method we'll invoke
		 * when the event fires.
		 * @param persistentClass the {@link Class} of the {@link Criteria} we're creating.
		 * Can be null.
		 * @param alias the alias of the {@link Criteria} we're creating.  Can be null.
		 * @param entityName the entity name of the {@link} Criteria we're creating.
		 * Can be null.
		 */
		private CriteriaFactoryImpl(MethodSig methodSig, System.Type persistentClass, string alias, string entityName)
		{
			this.methodSig = methodSig;
			this.persistentClass = persistentClass;
			this.alias = alias;
			this.entityName = entityName;
		}


		/**
		 * Create a CriteriaFactoryImpl.
		 *
		 * @param persistentClass the {@link Class} of the {@link Criteria} we're creating.
		 */
		public CriteriaFactoryImpl(System.Type persistentClass) : this(MethodSig.Class, persistentClass, null, null)
		{
		}

		/**
		 * Create a CriteriaFactoryImpl.
		 *
		 * @param persistentClass the {@link Class} of the {@link Criteria} we're creating.
		 * @param alias the alias of the {@link Criteria} we're creating.
		 */
		public CriteriaFactoryImpl(System.Type persistentClass, string alias)
			: this(MethodSig.ClassAndAlias, persistentClass, alias, null)
		{
		}

		/**
		 * Create a CriteriaFactoryImpl.
		 *
		 * @param entityName the entity name of the {@link Criteria} we're creating.
		 */
		public CriteriaFactoryImpl(string entityName) : this(MethodSig.Entity, null, null, entityName)
		{
		}

		/**
		 * Create a CriteriaFactoryImpl.
		 *
		 * @param entityName the entity name of the {@link Criteria} we're creating.
		 * @param alias the alias of the {@link Criteria} we're creating.
		 */
		public CriteriaFactoryImpl(string entityName, string alias) : this(MethodSig.EntityAndAlias, null, alias, entityName)
		{
		}

		public ICriteria CreateCriteria(ISession session)
		{
			switch (methodSig)
			{
				case MethodSig.Class:
					return session.CreateCriteria(persistentClass);
				case MethodSig.ClassAndAlias:
					return session.CreateCriteria(persistentClass, alias);
				case MethodSig.Entity:
					return session.CreateCriteria(entityName);
				case MethodSig.EntityAndAlias:
					return session.CreateCriteria(entityName, alias);
				default:
					throw new ShardedSessionException("Unknown constructor type for criteria create: " + methodSig);
			}
		}
	}
}