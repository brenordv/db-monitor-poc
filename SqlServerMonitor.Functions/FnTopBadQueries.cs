using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SqlServerMonitor.Core.Models;

namespace SqlServerMonitor.Functions;

public static class FnTopBadQueries
{
    [FunctionName("FnTopBadQueries")]
    public static async Task RunAsync(
        [TimerTrigger("0 */5 * * * *", RunOnStartup = true)]
        TimerInfo myTimer,
        [Sql(
            commandText: "%TopBadQueriesQuery%",
            commandType: CommandType.Text,
            connectionStringSetting: "SqlConnectionString")]
        IEnumerable<BadQueryInfo> queries,
        ILogger log)
    {
        var badQueries = queries.ToList();
        if (!badQueries.Any())
        {
            Console.WriteLine("No long running queries found.");
            return;
        }

        Console.WriteLine($"Found {badQueries.Count} long running queries.");
        badQueries.ForEach(Console.WriteLine);
    }
}