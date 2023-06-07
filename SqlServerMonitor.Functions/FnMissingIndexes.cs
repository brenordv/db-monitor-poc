using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SqlServerMonitor.Core.Builders;
using SqlServerMonitor.Core.Enums;
using SqlServerMonitor.Core.Models;
using SqlServerMonitor.Core.Senders;

namespace SqlServerMonitor.Functions;

public static class FnMissingIndexes
{
    [FunctionName("FnMissingIndexes")]
    public static async Task RunAsync(
        [TimerTrigger("0 */5 * * * *", RunOnStartup = true)]
        TimerInfo myTimer,
        [Sql(
            commandText: "%MissingIndexesQuery%",
            commandType: CommandType.Text,
            connectionStringSetting: "SqlConnectionString")]
        IEnumerable<MissingIndexInfo> missingIndexData,
        [CosmosDB(databaseName: "%OutDatabaseName%", containerName: "%outCollectionName%",
            Connection = "CosmosDbConnectionString",
            CreateIfNotExists = false,
            SqlQuery = "SELECT TOP 1 * FROM c WHERE c.Type = 2 ORDER BY c.CreatedAt DESC"
        )]IEnumerable<DbReportDocument> previousReports,
        ILogger log)
    {
        var reportBuilder = new QueryResultReporterBuilder();

        var (reportMessage, _) = reportBuilder
            .Add(missingIndexData, ReportType.MissingIndex, "Missing indexes")
            .Build();

        //TODO: Compare current report with previous report and add comparative data.
        
        //Could also send this Slack, Teams, etc.
        new ConsoleWriterSender().Send(reportMessage);
    }
}