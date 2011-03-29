using System;
using NHibernate.Shards.Util;
using NHibernate.Tool.hbm2ddl;

namespace NHibernate.Shards.Tool
{
    using NHibernate.Cfg;

    public class ShardedSchemaExport
    {
        private readonly ShardedConfiguration shardedConfiguration;
        private string delimiter;
        private string outputFile;

        public ShardedSchemaExport(ShardedConfiguration shardedConfiguration)
        {
            Preconditions.CheckNotNull(shardedConfiguration);
            this.shardedConfiguration = shardedConfiguration;
        }
        
        public void Create(Action<string> scriptAction, bool export)
        {
            ForEachShard(e => e.Create(scriptAction, export));
        }

        public void Create(bool script, bool export)
        {
            ForEachShard(e => e.Create(script, export));
        }

        public void Drop(bool script, bool export)
        {
            ForEachShard(e => e.Drop(script, export));
        }

        public void Execute(Action<string> scriptAction, bool export, bool justDrop)
        {
            ForEachShard(e => e.Execute(scriptAction, export, justDrop));
        }

        public void Execute(bool script, bool export, bool justDrop)
        {
            ForEachShard(e => e.Execute(script, export, justDrop));
        }

        public ShardedSchemaExport SetDelimiter(string value)
        {
            this.delimiter = value;
            return this;
        }

        public ShardedSchemaExport SetOutputFile(string value)
        {
            this.outputFile = value;
            return this;
        }

        private void ForEachShard(Action<SchemaExport> exportAction)
        {
            this.shardedConfiguration.ForEachShard(cfg =>
            {
                var schemaExport = CreateSchemaExport(cfg);
                exportAction(schemaExport);
            });
        }

        private SchemaExport CreateSchemaExport(Configuration config)
        {
            var result = new SchemaExport(config);
            if (!string.IsNullOrEmpty(this.delimiter)) result.SetDelimiter(this.delimiter);
            if (!string.IsNullOrEmpty(this.outputFile)) result.SetOutputFile(this.outputFile);
            return result;
        }
    }
}
