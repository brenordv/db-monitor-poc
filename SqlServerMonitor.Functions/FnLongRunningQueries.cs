using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using SqlServerMonitor.Core.Models;

namespace SqlServerMonitor.Functions;

public static class FnLongRunningQueries
{
    [FunctionName("FnLongRunningQueries")]
    public static async Task RunAsync(
        [TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo myTimer, 
        [Sql(
            commandText: "%LongRunningQuery%",
            commandType: System.Data.CommandType.Text,
            parameters: "@maxTime=%MaxQueryTimeInMs%",
            connectionStringSetting: "SqlConnectionString")]
        IEnumerable<LongRunningQueryInfo> queries, // Already have the bad queries, no need for extra actions.
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