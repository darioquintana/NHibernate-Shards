namespace NHibernate.Shards.Query
{
    public class AdHocQueryFactoryImpl : IQueryFactory
    {
        private readonly string queryString;

        public AdHocQueryFactoryImpl(string queryString)
        {
            this.queryString = queryString;
        }

        public IQuery CreateQuery(ISession session)
        {
            return session.CreateQuery(queryString);
        }
    }
}
