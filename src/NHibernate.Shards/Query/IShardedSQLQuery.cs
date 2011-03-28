namespace NHibernate.Shards.Query
{
    /// <summary>
    /// <see cref="IShardedSQLQuery"/> extends the <see cref="ISQLQuery"/> interface 
    /// to provide the ability to query across shards.
    /// </summary>
    public interface IShardedSQLQuery : IShardedQuery, ISQLQuery
    {}
}
