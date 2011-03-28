using System.Collections.Generic;

namespace NHibernate.Shards.Strategy.Exit
{
    public interface IListExitStrategy<T>: IExitStrategy<IEnumerable<T>>
    {
    }
}
