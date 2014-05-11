# Elasticsearch Replication Bundle for RavenDB

RavenDB is not suitable for reporting and ad-hoc querying. Elasticsearch on the other hand shines at excaly that, with its built-in capabilities for performing real-time aggregations on huge amounts of data.

This RavenDB bundle (or: plugin) makes replication from RavenDB to Elasticsearch possible, and very easy.

## Installation

With an instance of RavenDB 2.5, compile and drop the generated `Raven.Bundles.ElasticsearchReplication.dll` and its dependency `Elasticsearch.Net.dll` to the `Plugins` folder of the server.

Restart the RavenDB server, and create the replication configuration documents (see `Program.cs` in the demo application for example).

Each configuration document contains specifications to the RavenDB `Collection` to work on, the target Elasticsearch cluster, and a replication script to map from RavenDB documents to Elasticsearch data.

## Run the demo

First, have Elasticsearch running. You can connect to an existing Elasticsearch cluster if you want. The demo will create one index called `ravendb-elasticsearch-replication-demo`.

To run Elasticsearch locally follow these instructions:

1. Download from http://elasticsearch.org/download
2. Go edit config\elasticsearch.yml and edit:
	* `cluster.name` to something non-default. Your GitHub username will do.
	* replica and shard count, by specifing `index.number_of_shards: 1` and `index.number_of_replicas: 0` (unless you know what you are doing)
3. Make sure you have JAVA_HOME properly set up
4. Run `elasticsearch\bin\elasticsearch.bat`

### Run the Demo application

The Demo application is in fact an in-memory RavenDB instance that periodically and randomally adds `Order` and `ShoppingCart` objects to the system.

This RavenDB instance has the Elasticsearch Replication Bundle enabled, and it will replicate to Elasticsearch listening on `http://localhost:9200` by default.

### Start Kibana

If you have Kibana accessible on your cluster (either as a plugin or via some admin website), access it.

Otherwise, you can compile and run `Kibana.Host` in the demo project and it will provide you full blown Kibana pointing at `http://localhost:9200` (the Elasticsearch default). This Kibana interface will be available on `http://localhost:3579`.

### Access the sample Kibana dashboard

Once you have run Kibana, accessing it will probably get you to the Kibana welcome screen, unless you have previously installed Kibana and set a different home dashboard. You can start there, playing with Kibana yourself, or you can load the sample dashboard the Demo application has created for you.

To load the sample dashboard created for you by the Demo, go to the upper-right corner of Kibana, click Load, and from there select "RavenDB-Orders-Overview". This will load the sample dashboard which will look like this:

![Demo Kibana Dashboard](https://cloud.githubusercontent.com/assets/212252/2938891/06b01378-d927-11e3-9af7-507d68497975.PNG)

You can play with the dashboard by changing the queries, setting different time spans (via the date picker at the top or by selecting periods in the histogram) and changing the panels by clicking the cog icons.

### Cleaning up

To clear the demo data after the run send an HTTP DELETE command to `http://elasticsearch_host/ravendb-elasticsearch-replication-demo`. Or delete the data folder if working locally, that works too.

## Replication configurations

```csharp
new ElasticsearchReplicationConfig
{
	Id = "Raven/ElasticsearchReplication/Configuration/OrdersAndLines",
    Name = "OrdersAndLines", // name of the replication config

    // list of elasticsearch nodes in the cluster, at least 1 is required
    ElasticsearchNodeUrls = new List<Uri>{new Uri(ElasticsearchUrl)},

    // name of the index to replicate to
	IndexName = "ravendb-elasticsearch-replication-demo",

	// Name of RavenDB collection to execute the replication script on
	RavenEntityName = "Orders",

	// Some configurations to Elasticsearch types from RavenDB collections
	// For each replicated types, we need to specify where the RavenDB
	// document ID will reside, so we can cleanse it when we replicate
	// deletions or updates.
	// Ignore the Sql prefix, just some code reuse, it will be removed soon
	SqlReplicationTables =
	{
		// Configuration for the type to hold the main document being replicated
		new SqlReplicationTable
		{
			TableName = "Orders",
			// _id means this is the original document being replicated
			DocumentKeyColumn = "_id"
		},

		// Replicating a flat structure by mapping an array of objects
		// to their own Elasticsearch type.
		// This is not required with Elasticsearch, but sometimes will
		// be desired
		new SqlReplicationTable
		{
			TableName = "OrderLines",
			DocumentKeyColumn = "OrderId"
		},
	},

	// The replication script, see notes below
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
}
```

### The replication script

The replication script is just Javascript, executed in protected mode. Pretty much everything you can do in Javascript, you can do here.

There are several important conventions to note:

1. `replicateTo<type_name>(<data_object>)` is called to create a JSON document out of the `data_object` parameter and push it as a document to Elasticsearch under the configured index name and type `type_name`.

2. All Kibana dashboard expect a timespan field, which can be configured, but is defaulted to `@timestamp`. You can define the `@timestamp` field by using the proper Javascript notation `$timestamp = ...`. If not timestamp was specified by script, `DateTimeOffset.UtcNow` will be automatically set as the document timestamp by the bundle.

3. Elasticsearch will automatically detect numerics, dates and boolean values and index them appropriately. Strings require careful handling, as described below.

### String indexing

By default Elasticsearch indexes all strings as `analyzed`, meaning they will be tokenized and searchable on individual words. This also means for many types of data, things will not work they way you expect them to (for example: faceting on a string value).

To avoid that, the bundle provides a default mapping that tells Elasticsearch not to analyze string values unless their field name is prefixed with `_analyzed`. So, assuming you have this in the replication script:

`
...
ProductName = this.ProductName,
ProductName_analyzed = this.ProductName,
...
`

The `ProductName` field will be neither tokenized nor analyzed. While it can be searched on, only exact term lookups will be possible. But, it will behave as you'd expect on term facets (that is, like in the Top Products graph shown in the above screenshot).

The `ProductName_analyzed` field, on the other hand, will behave differently with facet operations (each individual word will be count as a value), but it could then be searched on like you'd expect, not requiring exact matches. Just make sure you pay attention to the [analyzer used](http://www.elasticsearch.org/guide/en/elasticsearch/reference/current/analysis-analyzers.html).

The mapping will not be applied by default. If you are interested in this (recommended) behavior, make sure to apply the mapping by calling the following code, _before_ starting replication to the Elasticsearch cluster:

```csharp
var cfg = new ElasticsearchReplicationConfig { ... }
cfg.SetupDefaultMapping();
```

## Kibana references

See http://www.elasticsearch.org/overview/kibana/ to get an idea of what Kibana is.