using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Elasticsearch.Net;
using Elasticsearch.Net.Connection;
using Elasticsearch.Net.ConnectionPool;

namespace Raven.Bundles.ElasticsearchReplication
{
    public static class Helpers
    {
        public static ElasticsearchClient GetElasticClient(this ElasticsearchReplicationConfig cfg)
        {
            var elasticsearchNodes = cfg.ElasticsearchNodeUrls;
            var connectionPool = elasticsearchNodes.Count == 1 ? (IConnectionPool)
                new SingleNodeConnectionPool(elasticsearchNodes.First()) : new StaticConnectionPool(elasticsearchNodes);
            return new ElasticsearchClient(new ConnectionConfiguration(connectionPool));
        }

        public static string GetEmbeddedJson(Assembly assembly, string embeddedResourcePath)
        {
            if (!embeddedResourcePath.StartsWith(".")) embeddedResourcePath = "." + embeddedResourcePath;

            using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + embeddedResourcePath))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static void SetupDefaultMapping(this ElasticsearchReplicationConfig cfg)
        {
            var assembly = typeof (ElasticsearchReplicationTask).Assembly;
            var template = GetEmbeddedJson(assembly, ".DefaultIndexTemplate.json");
            if (string.IsNullOrWhiteSpace(template))
                throw new Exception("Can't find the mapping");

            var client = GetElasticClient(cfg);
            var rsp = client.IndicesPutTemplateForAll("ravendb-elasticsearch-replication", template);
            if(!rsp.Success)
                throw new Exception("Error while trying to put a new template", rsp.OriginalException);
        }

        public static void AddKibanaDashboard(this ElasticsearchReplicationConfig cfg, string dashboardName,
            string dashboardSource)
        {
            var client = GetElasticClient(cfg);
            var rsp = client.Index("kibana-int", "dashboard", dashboardName, dashboardSource);
            if (!rsp.Success)
                throw new Exception("Error while trying to put Kibana dashboard " + dashboardName, rsp.OriginalException);
        }
    }
}
