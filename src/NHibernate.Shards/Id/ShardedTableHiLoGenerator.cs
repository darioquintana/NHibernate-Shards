using System;
using NHibernate.Engine;
using NHibernate.Id;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Id
{
    internal class ShardedTableHiLoGenerator : TableHiLoGenerator, IGeneratorRequiringControlSessionProvider
    {
        private IControlSessionProvider controlSessionProvider;

        public void SetControlSessionProvider(IControlSessionProvider provider)
        {
            controlSessionProvider = provider;
        }

        public override object Generate(ISessionImplementor session, Object obj)
        {
            if (controlSessionProvider != null)
            {
                using (var controlSession = controlSessionProvider.OpenControlSession())
                {
                    return base.Generate(controlSession.GetSessionImplementation(), obj);
                }
            }

            // Fallback behaviour defaults to regular TableHiLoGenerator, if not initialized 
            // for use in sharded environment.
            return base.Generate(session, obj);
        }
    }
}
