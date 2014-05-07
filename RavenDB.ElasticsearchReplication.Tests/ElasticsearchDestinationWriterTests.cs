using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raven.Database;
using Raven.Database.Bundles.ElasticsearchReplication;
using Raven.Database.Bundles.SqlReplication;
using Raven.Database.Config;
using Raven.Json.Linq;

namespace RavenDB.ElasticsearchReplication.Tests
{
    [TestClass]
    public class ElasticsearchDestinationWriterTests
    {
        [TestMethod]
        public void Replicating_items_should_generate_bulk_commands()
        {
            using (var writer = new WriterWrapperForTests())
            {
                writer.InsertItems("foo", "bar", new List<ItemToReplicate>
                {
                    new ItemToReplicate {Columns = new RavenJObject {{"test1", "value"}}, DocumentId = "products/1"}
                });

                Assert.AreEqual(2, writer.BulkCommands.Count);

                if (writer.Commit())
                    Assert.AreEqual(0, writer.BulkCommands.Count);
            }
        }

        [TestMethod]
        public void Deleting_items_should_generate_bulk_commands()
        {
            using (var writer = new WriterWrapperForTests())
            {
                writer.DeleteItems("foo", "bar", new List<string>{"products/1"});

                Assert.AreEqual(1, writer.BulkCommands.Count);

                if (writer.Commit())
                    Assert.AreEqual(0, writer.BulkCommands.Count);
            }
        }

        private class WriterWrapperForTests : ElasticsearchDestinationWriter
        {
            public WriterWrapperForTests()
                : base(null, new SqlReplicationConfig {ConnectionString = "http://localhost:9200"},
                new SqlReplicationStatistics("Elasticsearch Replication"))
            {

            }

            public new void InsertItems(string tableName, string pkName, IEnumerable<ItemToReplicate> dataForTable)
            {
                base.InsertItems(tableName, pkName, dataForTable);
            }

            public List<string> BulkCommands { get { return base.bulkCommands; }}
        }
    }
}
