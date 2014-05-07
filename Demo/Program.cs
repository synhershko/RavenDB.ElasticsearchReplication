using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Threading;
using FizzWare.NBuilder.Generators;
using Raven.Client.Embedded;
using Raven.Database.Bundles.ElasticsearchReplication;
using Raven.Database.Bundles.SqlReplication;

namespace Demo
{
    class Program
    {
        public static string ElasticsearchUrl = "http://localhost:9200";

        private static Timer timer;

        static void Main(string[] args)
        {
            using (var store = new EmbeddableDocumentStore
            {
                UseEmbeddedHttpServer = true,
                Configuration =
                {
                    RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true,
                    RunInMemory = true,
                },
            })
            {
                store.Configuration.Catalog.Catalogs.Add(new AssemblyCatalog(typeof(ElasticsearchReplicationTask).Assembly));
                store.Configuration.Settings.Add("Raven/ActiveBundles", "ElasticsearchReplication"); // Enable the bundle
                store.Initialize();

                using (var session = store.OpenSession())
                {
                    var cfg = new ElasticsearchReplicationConfig
                    {
                        Id = "Raven/ElasticsearchReplication/Configuration/OrdersAndLines",
                        Name = "OrdersAndLines",
                        ElasticsearchNodeUrls = new List<Uri>{new Uri(ElasticsearchUrl)},
                        IndexName = "ravendb-elasticsearch-replication-demo",
                        RavenEntityName = "Orders",
                        SqlReplicationTables =
                        {
                            new SqlReplicationTable
                            {
                                TableName = "Orders",
                                DocumentKeyColumn = "_id"
                            },
                            new SqlReplicationTable
                            {
                                TableName = "OrderLines",
                                DocumentKeyColumn = "OrderId"
                            },
                        },
                        Script = @"
        var orderData = {
            Id: documentId,
            OrderLinesCount: this.OrderLines.length,
            CreatedAt: this.CreatedAt,
            CustomerName: this.CustomerName,
            TotalCost: 0
        };
 
        for (var i = 0; i < this.OrderLines.length; i++) {
            var line = this.OrderLines[i];
            orderData.TotalCost += (line.UnitPrice * line.Quantity);
 
            replicateToOrderLines({
                OrderId: documentId,
                PurchasedAt: orderData.CreatedAt,
                Quantity: line.Quantity,
                UnitPrice: line.UnitPrice,
                ProductId: line.ProductId
            });
        }

        replicateToOrders(orderData);"
                    };

                    // Sets up the bundle's default Elasticsearch mapping
                    cfg.SetupDefaultMapping();
                    
                    session.Store(cfg);
                    session.SaveChanges();
                }

                timer = new Timer(state =>
                {
                    using (var session = store.OpenSession())
                    {
                        for (int i = 0; i < GetRandom.Int(0, 5); i++)
                        {
                            session.Store(FakeDataGenerator.CreateAFakeOrder());
                        }
                        session.SaveChanges();
                    }
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

                // TODO add Kibana dashboard settings

                Console.WriteLine("Posting Orders randomly to an in-memory RavenDB instance");
                Console.WriteLine("That instance is then replicating some of that data to an Elasticsearch instance on " + ElasticsearchUrl);
                Console.WriteLine("Run Kibana (available via Kibana.Host in the same solution as this Demo app) to view real-time analytics");
                Console.WriteLine("Working in the background.. press any key to quit");
                Console.ReadKey();
            }
        }
    }
}
