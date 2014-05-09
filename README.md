# Elasticsearch Replication Bundle for RavenDB

## Installation

## Run the demo

First, have Elasticsearch running:

1. Download from http://elasticsearch.org/download
2. Go edit config\elasticsearch.yml and edit:
	* `cluster.name` to something non-default
	* replica and shard count, by specifing `index.number_of_shards: 1` and `index.number_of_replicas: 0` (unless you know what you are doing)
3. Make sure you have JAVA_HOME properly set up
4. Run `elasticsearch\bin\elasticsearch.bat`

### Run the Demo application

The Demo application is in fact an in-memory RavenDB instance that periodically and randomally adds `Order` objects to the system.

This RavenDB instance has the Elasticsearch Replication Bundle enabled, and it will replicate to Elasticsearch listening on `http://localhost:9200` by default.

### Start Kibana

If you have Kibana accessible on your cluster (either as a plugin or via some admin website), access it.

Otherwise, you can compile and run Kibana.Host and it will provide you full blown Kibana pointing at `http://localhost:9200` (the Elasticsearch default). This Kibana interface will be available on `http://localhost:3579`.
