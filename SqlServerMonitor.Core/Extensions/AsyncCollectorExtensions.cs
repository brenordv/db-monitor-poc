using Microsoft.Azure.WebJobs;
using SqlServerMonitor.Core.Models;

namespace SqlServerMonitor.Core.Extensions;

public static class AsyncCollectorExtensions
{
    public static async Task AddRangeAsync(this IAsyncCollector<DbReportDocument> collection, IEnumerable<DbReportDocument> documents)
    {
        foreach (var document in documents)
        {
            await collection.AddAsync(document);    
        }
    }
    
}