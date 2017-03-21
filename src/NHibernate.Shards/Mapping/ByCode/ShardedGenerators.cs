namespace NHibernate.Shards.Mapping.ByCode
{
    using NHibernate.Mapping.ByCode;

    public static class ShardedGenerators
    {
        private static IGeneratorDef shardedUUIDGenerator;
        private static IGeneratorDef shardedHiLoGenerator;

        public static IGeneratorDef ShardedUUIDGenerator()
        {
            return shardedUUIDGenerator  ?? (shardedUUIDGenerator = new ShardedUUIDGeneratorDef());
        }

        public static IGeneratorDef ShardedHighLowGenerator()
        {
            return shardedHiLoGenerator ?? (shardedHiLoGenerator = new ShardedHighLowGeneratorDef());
        }
    }
}
