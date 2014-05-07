using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elasticsearch.Net;
using Raven.Abstractions;
using Raven.Abstractions.Data;
using Raven.Abstractions.Logging;
using Raven.Database.Bundles.SqlReplication;
using Raven.Database.Extensions;
using Raven.Imports.Newtonsoft.Json;
using Raven.Json.Linq;

namespace Raven.Database.Bundles.ElasticsearchReplication
{
    public class ElasticsearchDestinationWriter :  IDisposable
    {
        private readonly DocumentDatabase database;
		private readonly SqlReplicationConfig cfg;
		private readonly SqlReplicationStatistics replicationStatistics;

        private readonly string targetIndexName;
        protected readonly ElasticsearchClient elasticsearchClient;

        protected readonly List<DeleteByQueryCommand> deleteByQueryCommands = new List<DeleteByQueryCommand>();
        protected readonly List<string> bulkCommands = new List<string>();

		private static readonly ILog log = LogManager.GetCurrentClassLogger();

		bool hadErrors;

        public ElasticsearchDestinationWriter(DocumentDatabase database, SqlReplicationConfig _cfg, SqlReplicationStatistics replicationStatistics)
		{
            var cfg = new ElasticsearchReplicationConfig(_cfg);

			this.database = database;
			this.cfg = cfg;
            this.targetIndexName = cfg.FactoryName.ToLowerInvariant(); // Elasticsearch requires all index names to be lowercased
			this.replicationStatistics = replicationStatistics;
            
            try
            {
                elasticsearchClient = cfg.GetElasticClient();
            }
            catch (UriFormatException e)
            {
                if (database != null)
                    database.AddAlert(new Alert
                    {
                        AlertLevel = AlertLevel.Error,
                        CreatedAt = SystemTime.UtcNow,
                        Exception = e.ToString(),
                        Title = "Invalid Elasticsearch URL provided",
                        Message = "Elasticsearch Replication could not parse one of the provided node URLs",
                        UniqueKey = "Elasticsearch Replication Connection Error: " + cfg.ConnectionString
                    });
            }
			catch (Exception e)
			{
			    if (database != null)
			        database.AddAlert(new Alert
			        {
			            AlertLevel = AlertLevel.Error,
			            CreatedAt = SystemTime.UtcNow,
			            Exception = e.ToString(),
			            Title = "Elasticsearch Replication could not open connection",
			            Message = "Elasticsearch Replication could not open connection to " + cfg.ConnectionString,
			            UniqueKey = "Elasticsearch Replication Connection Error: " + cfg.ConnectionString
			        });
				throw;
			}
		}

		public bool Execute(ConversionScriptResult scriptResult)
		{
			var identifiers = scriptResult.Data.SelectMany(x => x.Value).Select(x => x.DocumentId).Distinct().ToList();
			foreach (var sqlReplicationTable in cfg.SqlReplicationTables)
			{
				// first, delete all the rows that might already exist there
				DeleteItems(sqlReplicationTable.TableName, sqlReplicationTable.DocumentKeyColumn, identifiers);
			}

			foreach (var sqlReplicationTable in cfg.SqlReplicationTables)
			{
				List<ItemToReplicate> dataForTable;
				if (scriptResult.Data.TryGetValue(sqlReplicationTable.TableName, out dataForTable) == false)
					continue;

				InsertItems(sqlReplicationTable.TableName, sqlReplicationTable.DocumentKeyColumn, dataForTable);
			}

			Commit();

			return hadErrors == false;
		}

		public bool Commit()
		{
            try
            {                
                // TODO support controlling bulk sizes

                foreach (var deleteByQueryCommand in deleteByQueryCommands)
                {
                    var rsp = elasticsearchClient.DeleteByQuery(deleteByQueryCommand.IndexName, deleteByQueryCommand.Command);
                    if (rsp.Success) continue;

                    var sb = new StringBuilder("Error while replicating to Elasticsearch on " + rsp.RequestUrl);
                    sb.Append(string.Format(" (HTTP status code {0}; response: {1})", rsp.HttpStatusCode, rsp.Response));
                    throw new Exception(sb.ToString(), rsp.OriginalException);
                }
                deleteByQueryCommands.Clear();

                var response = elasticsearchClient.Bulk(bulkCommands);
                if (!response.Success)
                {
                    var sb = new StringBuilder("Error while replicating to Elasticsearch on " + response.RequestUrl);
                    sb.Append(string.Format(" (HTTP status code {0}; response: {1})", response.HttpStatusCode, response.Response));
                    throw new Exception(sb.ToString(), response.OriginalException);
                }
                bulkCommands.Clear();
            }
            catch (Exception e)
            {
                log.WarnException(
                    "Failure to replicate changes to Elasticsearch for: " + cfg.Name + ", will continue trying.", e);
                replicationStatistics.RecordWriteError(e, database);
                hadErrors = true;
                return false;
            }
			return true;
		}

		protected void InsertItems(string tableName, string pkName, IEnumerable<ItemToReplicate> dataForTable)
		{
		    tableName = tableName.ToLowerInvariant(); // type names have to be lowercase
            foreach (var itemToReplicate in dataForTable)
            {
                var o = new RavenJObject();

                if (database != null)
                    database.WorkContext.CancellationToken.ThrowIfCancellationRequested();

                foreach (var column in itemToReplicate.Columns.Where(column => column.Key != pkName))
                {
                    o[column.Key] = column.Value;
                }

                if ("_id".Equals(pkName))
                {
                    bulkCommands.Add("{\"index\":{\"_index\":\"" + targetIndexName + "\",\"_type\":\"" + tableName + "\",\"_id\":\"" + itemToReplicate.DocumentId + "\"}}");
                }
                else
                {
                    o[pkName] = itemToReplicate.DocumentId;
                    bulkCommands.Add("{\"index\":{\"_index\":\"" + targetIndexName + "\",\"_type\":\"" + tableName + "\"}}");
                }

                bulkCommands.Add(o.ToString(Formatting.None));
            }
		}

        public void DeleteItems(string tableName, string pkName, List<string> identifiers)
        {
            tableName = tableName.ToLowerInvariant(); // type names have to be lowercase

            if (database != null)
                database.WorkContext.CancellationToken.ThrowIfCancellationRequested();

            if ("_id".Equals(pkName))
            {
                foreach (var identifier in identifiers)
                {
                    bulkCommands.Add("{\"delete\":{\"_index\":\"" + targetIndexName + "\",\"_type\":\"" + tableName +
                                     "\",\"_id\":\"" + identifier + "\"}}");
                }
            }
            else
            {
                // TODO
                new DeleteByQueryCommand {IndexName = targetIndexName, Command = ""};
            }

        }

		public void Dispose()
		{

		}
    }
}
