using System;

namespace NHibernate.Shards.Criteria
{
    public class SetFetchModeEvent : ICriteriaEvent
    {
        private readonly String associationPath;

        // the FetchMode that will be set on the Criteria
        private readonly FetchMode mode;
        
        /**
         * Construct a SetFetchModeEvent
         *
         * @param associationPath the association path of the fetch mode
         * we'll set on the {@link Criteria} when the event fires.
         * @param mode the mode we'll set on the {@link Criteria} when the event fires.
         */
        public SetFetchModeEvent(String associationPath, FetchMode mode) 
        {
          this.associationPath = associationPath;
          this.mode = mode;
        }
        
        public void OnEvent(ICriteria crit) 
        {
          crit.SetFetchMode(associationPath, mode);
        }

    }
}
