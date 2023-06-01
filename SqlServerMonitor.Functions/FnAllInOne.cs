using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SqlServerMonitor.Core.Builders;
using SqlServerMonitor.Core.Enums;
using SqlServerMonitor.Core.Extensions;
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
        )]IAsyncCollector<DbReportDocument> documentsOut,
        ILogger log)
    {
        //TODO: Get previous reports from CosmosDB and add comparative data.
        
        var reportBuilder = new QueryResultReporterBuilder();

        var (reportMessage, reportDocument) = reportBuilder
            .Add(longRunningQueries, ReportType.LongRunningQuery, "Long running queries")
            .Add(missingIndexData, ReportType.MissingIndex, "Missing indexes")
            .Add(topBadQueries, ReportType.BadQuery, "Top bad queries")
            .Build();
        
        //Could also send this Slack, Teams, etc.
        new ConsoleWriterSender().Send(reportMessage);

        //Add the reports to CosmosDB
        await documentsOut.AddRangeAsync(reportDocument);
    }
}