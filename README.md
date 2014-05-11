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

## Kibana references

See http://www.elasticsearch.org/overview/kibana/ to get an idea of what Kibana is.