using NHibernate.Shards.Util;

namespace NHibernate.Shards.Strategy.Exit
{
    public class ExecuteUpdateExitStrategy : IExitStrategy<int>
	{
        private int result;

        #region IExitStrategy<int> Members

        public bool AddResult(int oneResult, IShard shard)
        {
            InterlockedUtil.Add(ref result, oneResult);
            return false;
        }

        public int CompileResults()
        {
            return result;
        }

        #endregion
    }
}
