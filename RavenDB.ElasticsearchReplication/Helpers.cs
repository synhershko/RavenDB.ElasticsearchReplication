using System;
using System.IO;
using System.Linq;
using Elasticsearch.Net;
using Elasticsearch.Net.Connection;
using Elasticsearch.Net.ConnectionPool;

namespace Raven.Database.Bundles.ElasticsearchReplication
{
    public static class Helpers
    {
        internal static ElasticsearchClient GetElasticClient(this ElasticsearchReplicationConfig cfg)
        {
            var elasticsearchNodes = cfg.ElasticsearchNodeUrls;
            var connectionPool = elasticsearchNodes.Count == 1 ? (IConnectionPool)
                new SingleNodeConnectionPool(elasticsearchNodes.First()) : new StaticConnectionPool(elasticsearchNodes);
            return new ElasticsearchClient(new ConnectionConfiguration(connectionPool));
        }

        public static void SetupDefaultMapping(this ElasticsearchReplicationConfig cfg)
        {
            var assembly = typeof (ElasticsearchReplicationTask).Assembly;
            string template;
            using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".DefaultIndexTemplate.json"))
            using (var reader = new StreamReader(stream))
            {
                template = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(template))
                throw new Exception("Can't find the mapping");

            var client = GetElasticClient(cfg);
            var rsp = client.IndicesPutTemplateForAll("ravendb-elasticsearch-replication", template);
            if(!rsp.Success)
                throw new Exception("Error while trying to put a new template", rsp.OriginalException);
        }
    }
}
