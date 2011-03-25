using NHibernate.Transform;

namespace NHibernate.Shards.Criteria
{
	/**
	 * Event that allows the {@link ResultTransformer} of a {@link Criteria} to be set lazily.
	 * @see Criteria#setResultTransformer(ResultTransformer)
	 *
	 * @author maxr@google.com (Max Ross)
	 */
    class SetResultTransformerEvent:ICriteriaEvent
    {
		// the resultTransformer we'll set on the Critieria when the event fires
        private readonly IResultTransformer resultTransformer;

		/**
		 * Constructs a SetResultTransformerEvent
		 *
		 * @param resultTransformer the resultTransformer we'll set on the {@link Criteria} when
		 * the event fires.
		 */
        public SetResultTransformerEvent(IResultTransformer resultTransformer)
        {
            this.resultTransformer = resultTransformer;
        }

        #region Implementation of ICriteriaEvent

        public void OnEvent(ICriteria crit)
        {
            crit.SetResultTransformer(resultTransformer);
        }

        #endregion
    }
}
