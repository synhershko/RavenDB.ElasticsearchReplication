using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Raven.Database.Bundles.SqlReplication;

namespace Raven.Database.Bundles.ElasticsearchReplication
{
    public class ElasticsearchReplicationConfig : SqlReplicationConfig
    {
        public ElasticsearchReplicationConfig(SqlReplicationConfig other)
        {
            this.ConnectionString = other.ConnectionString;
            this.FactoryName = other.FactoryName;
            this.Script = other.Script;
            this.Disabled = other.Disabled;
            this.Id = other.Id;
            this.Name = other.Name;
            this.RavenEntityName = other.RavenEntityName;
            this.SqlReplicationTables = other.SqlReplicationTables;
        }

        public ElasticsearchReplicationConfig()
        {            
        }

        /// <summary>
        /// URLs to Elasticsearch nodes; needless to say all nodes are expected to be on the same cluster
        /// One node is fine, knowing about additional nodes in the cluster is just to help with high-availability
        /// </summary>
        public List<Uri> ElasticsearchNodeUrls
        { // this is infact hijacking the SqlReplication connection string property
            get
            {
                if (ConnectionString == null) return new List<Uri>();
                var urls = ConnectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                return urls.Select(url => new Uri(url)).ToList();
            }
            set
            {
                var sb = new StringBuilder();
                foreach (var uri in value)
                {
                    sb.Append(uri);
                    sb.Append(';');
                }
                ConnectionString = sb.ToString();
            }
        }

        // TODO support rolling indexes

        // TODO allow to define mappings

        /// <summary>
        /// Index name to replicate to. Data will be replicated to individual types within this index.
        /// </summary>
        public string IndexName
        { 
            get { return FactoryName; }
            set
            {
                if (value.Any(c => !Char.IsLower(c) && Char.IsLetter(c)))
                {
                    throw new ArgumentException("Index name has to be lowercase, but may contain characters like hyphen");
                }
                FactoryName = value;
            }
        }
    }
}
