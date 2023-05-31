using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SqlServerMonitor.Core.Models;

namespace SqlServerMonitor.Functions;

public static class FnMissingIndexes
{
    [FunctionName("FnMissingIndexes")]
    public static async Task RunAsync(
        [TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo myTimer, 
        [Sql(
            commandText: "%MissingIndexesQuery%",
            commandType: System.Data.CommandType.Text,
            connectionStringSetting: "SqlConnectionString")]
        IEnumerable<MissingIndexInfo> missingIndexData,
        ILogger log)
    {
        var missingIndexes = missingIndexData.ToList();
        if (!missingIndexes.Any())
        {
            Console.WriteLine("No missing indexes detected.");
            return;
        }
        
        Console.WriteLine($"Found {missingIndexes.Count} missing indexes.");
        missingIndexes.ForEach(Console.WriteLine);
    }
}