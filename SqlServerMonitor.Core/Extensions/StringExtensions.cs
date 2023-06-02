using System.Text.RegularExpressions;
using SqlServerMonitor.Core.Models;

namespace SqlServerMonitor.Core.Extensions;

public static class StringExtensions
{
    public static IList<RelOp> ExtractStatementsFromPlan(this string queryPlan)
    {
        var result = new List<RelOp>();

        if (string.IsNullOrWhiteSpace(queryPlan))
            return result;

        var matches = Regex.Matches(queryPlan,
            "<RelOp[^>]*PhysicalOp=\"([^\"]+)\"[^>]*LogicalOp=\"([^\"]+)\"[^>]*EstimateRows=\"([\\d.]+)\"[^>]*EstimatedRowsRead=\"([\\d.]+)\"[^>]*EstimateIO=\"([\\d.]+)\"[^>]*EstimateCPU=\"([\\d.]+)\"[^>]*AvgRowSize=\"([\\d.]+)\"[^>]*EstimatedTotalSubtreeCost=\"([\\d.]+)\"[^>]*EstimatedExecutionMode=\"([^\"]+)\"");

        foreach (Match match in matches)
        {
            if (!double.TryParse(match.Groups[3].Value, out var estimateRows) ||
                !double.TryParse(match.Groups[4].Value, out var estimatedRowsRead) ||
                !double.TryParse(match.Groups[5].Value, out var estimateIO) ||
                !double.TryParse(match.Groups[6].Value, out var estimateCPU) ||
                !double.TryParse(match.Groups[7].Value, out var avgRowSize) ||
                !double.TryParse(match.Groups[8].Value, out var estimatedTotalSubtreeCost))
                continue;

            result.Add(new RelOp(
                match.Groups[1].Value,
                match.Groups[2].Value,
                estimateRows,
                estimatedRowsRead,
                estimateIO,
                estimateCPU,
                avgRowSize,
                estimatedTotalSubtreeCost,
                match.Groups[9].Value
            ));
        }

        if (!result.Any())
            return result;

        var averageCost = result.Average(r => r.EstimatedTotalSubtreeCost);

        return result
            .OrderByDescending(r => r.EstimatedTotalSubtreeCost)
            .Select(r => r with { IsAboveAverageCost = r.EstimatedTotalSubtreeCost >= averageCost })
            .ToList();
    }
}