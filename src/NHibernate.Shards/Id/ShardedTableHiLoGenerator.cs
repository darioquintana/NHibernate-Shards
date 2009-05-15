using System;
using System.Runtime.Serialization;
using NHibernate.Engine;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Id
{
    internal class ShardedTableHiLoGenerator : IGeneratorRequiringControlSessionProvider
    {
        private IControlSessionProvider controlSessionProvider;

        public void SetControlSessionProvider(IControlSessionProvider provider)
        {
            controlSessionProvider = provider;
        }

        public ISerializable Generate(ISessionImplementor session, Object obj)
        {
            ISerializable id;
            ISessionImplementor controlSession = null;
            try
            {
                controlSession = controlSessionProvider.OpenControlSession();
                id = SuperGenerate(controlSession, obj);
            }
            finally
            {
                if (controlSession != null)
                {
                    ((ISession) controlSession).Close();
                }
            }
            return id;
        }

        private ISerializable SuperGenerate(ISessionImplementor controlSession, Object obj)
        {
            return Generate(controlSession, obj);
        }
    }
}
