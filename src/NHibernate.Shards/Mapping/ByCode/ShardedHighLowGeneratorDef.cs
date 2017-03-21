using NHibernate.Mapping.ByCode;
using NHibernate.Shards.Id;

namespace NHibernate.Shards.Mapping.ByCode
{
    public class ShardedHighLowGeneratorDef : HighLowGeneratorDef, IGeneratorDef
    {
        public new string Class
        {
            get { return typeof(ShardedTableHiLoGenerator).AssemblyQualifiedName; }
        }
    }
}
