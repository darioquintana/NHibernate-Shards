using NHibernate.Shards.Session;
using NHibernate.SqlCommand;

namespace NHibernate.Shards.Criteria
{
	/// <summary>
	/// Event that allows an alias to be lazily added to a Criteria.
	/// @see Criteria#createAlias(String, String)
	/// @see Criteria#createAlias(String, String, int)
	/// @see Criteria#createAlias(String, String, int)
	/// </summary>
	public class CreateAliasEvent : ICriteriaEvent
    {
        private enum MethodSig
        {
            AssocPathAndAlias,
            AssocPathAndAliasAndJoinType
        }

		// the signature of the createAlias method we're going to invoke when
		// the event fires
        private readonly MethodSig methodSig;

		// the association path
        private readonly string associationPath;

		// the name of the alias we're creating
        private readonly string alias;

		// the join type - we look at method sig to see if we should use it
        private readonly JoinType joinType;

		/// <summary>
		/// Construct a CreateAliasEvent
		/// Construct a CreateAliasEvent
		/// </summary>
		/// <param name="methodSig">The signature of the createAlias method we're going to invoke when the event fires</param>
		/// <param name="associationPath">the association path of the alias we're creating</param>
		/// <param name="alias"> the name of the alias we're creating</param>
		/// <param name="joinType">the join type of the alias we're creating. Can be null</param>
		private CreateAliasEvent(MethodSig methodSig, string associationPath, string alias, JoinType joinType)
		{
			this.methodSig = methodSig;
			this.associationPath = associationPath;
			this.alias = alias;
			this.joinType = joinType;
		}

		/*
		 * Construct a CreateAliasEvent
		 *
		 * @param associationPath the association path of the alias we're creating.
		 * @param alias the name of the alias we're creating.
			*/		
		public CreateAliasEvent(string associationPath, string alias)
			: this(MethodSig.AssocPathAndAlias, associationPath, alias, JoinType.None)
		{
		}

		 /* Construct a CreateAliasEvent
		 *
		 * @param associationPath the association path of the alias we're creating.
		 * @param alias the name of the alias we're creating.
		 * @param joinType the join type of the alias we're creating.
		 */
		public CreateAliasEvent(string associationPath, string alias, JoinType joinType)
			: this(MethodSig.AssocPathAndAliasAndJoinType, associationPath, alias, joinType)
		{
		}

		#region Implementation of ICriteriaEvent

		public void OnEvent(ICriteria crit)
		{
			switch (methodSig)
			{
				case MethodSig.AssocPathAndAlias:
					crit.CreateAlias(associationPath, alias);
					break;
				case MethodSig.AssocPathAndAliasAndJoinType:
					crit.CreateAlias(associationPath, alias, joinType);
					break;
				default:
					throw new ShardedSessionException("Unknown ctor type in CreateAliasEvent: " + methodSig);
			}
		}

		#endregion
	}
}