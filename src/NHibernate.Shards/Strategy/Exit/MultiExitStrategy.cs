using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Exit
{
    public class MultiExitStrategy: IExitStrategy<IList>
    {
        private readonly IListExitStrategy<object>[] exitStrategies;

        public MultiExitStrategy(IEnumerable<IListExitStrategy<object>> exitStrategies)
        {
            Preconditions.CheckNotNull(exitStrategies);
            this.exitStrategies = exitStrategies.ToArray();
        }

        #region IExitStrategy<IList> Members

        public bool AddResult(IList result, IShard shard)
        {
            for (int i = 0; i < this.exitStrategies.Length; i++)
            {
                var exitStrategy = this.exitStrategies[i];
                if (exitStrategy != null)
                {
                    exitStrategy.AddResult(((IEnumerable)result[i]).Cast<object>(), shard);
                }
            }
            return false;
        }

        public IList CompileResults()
        {
            var result = new object[this.exitStrategies.Length];
            for (int i = 0; i < this.exitStrategies.Length; i++)
            {
                var exitStrategy = this.exitStrategies[i];
                if (exitStrategy != null)
                {
                    result[i] = exitStrategy.CompileResults();
                }
            }
            return result;
        }

        #endregion
    }
}
