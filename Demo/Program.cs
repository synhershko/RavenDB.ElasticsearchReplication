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

        private static Timer timer, timer2;

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

                var replicationConfigs = new List<ElasticsearchReplicationConfig>
                                         {
                                             new ElasticsearchReplicationConfig
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
            $timestamp: this.CreatedAt,
            CustomerName: this.CustomerName,
            TotalCost: 0
        };
 
        for (var i = 0; i < this.OrderLines.length; i++) {
            var line = this.OrderLines[i];
            orderData.TotalCost += (line.UnitPrice * line.Quantity);
 
            replicateToOrderLines({
                $timestamp: this.CreatedAt,
                OrderId: documentId,
                PurchasedAt: orderData.CreatedAt,
                Quantity: line.Quantity,
                UnitPrice: line.UnitPrice,
                ProductId: line.ProductId,
                ProductName: line.ProductName
            });
        }

        replicateToOrders(orderData);"
                    },
                    new ElasticsearchReplicationConfig
                    {
                        Id = "Raven/ElasticsearchReplication/Configuration/ShoppingCarts",
                        Name = "ShoppingCarts",
                        ElasticsearchNodeUrls = new List<Uri>{new Uri(ElasticsearchUrl)},
                        IndexName = "ravendb-elasticsearch-replication-demo",
                        RavenEntityName = "ShoppingCarts",
                        SqlReplicationTables =
                        {
                            new SqlReplicationTable
                            {
                                TableName = "ShoppingCarts",
                                DocumentKeyColumn = "_id"
                            },
                        },
                        Script = @"
        var data = {
            $timestamp: this.CreatedAt,
            IpAddress: this.IpAddress
        };

        replicateToShoppingCarts(data);"
                    }
                                         };
                using (var session = store.OpenSession())
                {
                    foreach (var cfg in replicationConfigs)
                    {
                        // Sets up the bundle's default Elasticsearch mapping. We call this separately on every config
                        // because potentially each config may replicate to a different Elasticsearch cluster
                        cfg.SetupDefaultMapping();

                        session.Store(cfg);
                    }
                    session.SaveChanges();
                }

                // Simulate orders and shopping carts logging using different threads / clients
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

                timer2 = new Timer(state =>
                {
                    using (var session = store.OpenSession())
                    {
                        for (int i = 0; i < GetRandom.Int(0, 10); i++)
                        {
                            session.Store(FakeDataGenerator.CreateAFakeShoppingCart());
                        }
                        session.SaveChanges();
                    }
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

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
