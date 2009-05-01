using NHibernate.Transform;

namespace NHibernate.Shards.Criteria
{
    public class SetResultTransformerEvent : ICriteriaEvent
    {
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

          public void OnEvent(ICriteria crit) 
          {
            crit.SetResultTransformer(resultTransformer);
          }

    }
}
