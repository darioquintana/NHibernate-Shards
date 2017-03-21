using NHibernate.Mapping.ByCode;
using NHibernate.Shards.Id;

namespace NHibernate.Shards.Mapping.ByCode
{
    public class ShardedUUIDGeneratorDef : UUIDHexGeneratorDef, IGeneratorDef
    {
        public new string Class
        {
            get { return typeof(ShardedUUIDGenerator).AssemblyQualifiedName; }
        }
    }
}
