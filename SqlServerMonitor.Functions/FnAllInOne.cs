using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SqlServerMonitor.Core.Build;
using SqlServerMonitor.Core.Models;
using SqlServerMonitor.Core.Senders;

namespace SqlServerMonitor.Functions;

public static class FnAllInOne
{
    [FunctionName("FnAllInOne")]
    public static async Task RunAsync(
        [TimerTrigger("0 */5 * * * *", RunOnStartup = true)]
        TimerInfo myTimer,
        [Sql(
            commandText: "%LongRunningQuery%",
            commandType: CommandType.Text,
            parameters: "@maxTime=%MaxQueryTimeInMs%",
            connectionStringSetting: "SqlConnectionString")]
        IEnumerable<LongRunningQueryInfo> longRunningQueries,
        [Sql(
            commandText: "%MissingIndexesQuery%",
            commandType: CommandType.Text,
            connectionStringSetting: "SqlConnectionString")]
        IEnumerable<MissingIndexInfo> missingIndexData,
        [Sql(
            commandText: "%TopBadQueriesQuery%",
            commandType: CommandType.Text,
            connectionStringSetting: "SqlConnectionString")]
        IEnumerable<BadQueryInfo> topBadQueries,
        [CosmosDB(databaseName: "%OutDatabaseName%", containerName: "%outCollectionName%",
            Connection = "CosmosDbConnectionString",
            CreateIfNotExists = false,
            PartitionKey = "/id"
        )]IAsyncCollector<dynamic> documentsOut,
        ILogger log)
    {
        var reportBuilder = new QueryResultReporterBuilder();

        reportBuilder.AddData(longRunningQueries);
        reportBuilder.AddData(missingIndexData);
        reportBuilder.AddData(topBadQueries);

        var (reportMessage, reportDocument) = reportBuilder.Build();

        //Could also send this Slack, Teams, etc.
        new ConsoleWriterSender().Send(reportMessage);

        await documentsOut.AddAsync(reportDocument);
    }
}