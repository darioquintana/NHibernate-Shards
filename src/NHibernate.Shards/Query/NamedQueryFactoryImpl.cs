namespace NHibernate.Shards.Query
{
    internal class NamedQueryFactoryImpl : IQueryFactory
    {
        private readonly string queryName;

        public NamedQueryFactoryImpl(string queryName)
        {
            this.queryName = queryName;
        }

        public IQuery CreateQuery(ISession session)
        {
            return session.GetNamedQuery(queryName);
        }
    }
}
