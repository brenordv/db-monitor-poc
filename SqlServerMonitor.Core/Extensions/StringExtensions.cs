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
        
        var matches = Regex.Matches(queryPlan, "<RelOp[^>]*PhysicalOp=\"([^\"]+)\"[^>]*LogicalOp=\"([^\"]+)\"[^>]*EstimateRows=\"([\\d.]+)\"[^>]*EstimatedRowsRead=\"([\\d.]+)\"[^>]*EstimateIO=\"([\\d.]+)\"[^>]*EstimateCPU=\"([\\d.]+)\"[^>]*AvgRowSize=\"([\\d.]+)\"[^>]*EstimatedTotalSubtreeCost=\"([\\d.]+)\"[^>]*EstimatedExecutionMode=\"([^\"]+)\"");

        foreach (var match in matches.Select(m => m.Groups))
        {
            result.Add(new RelOp(
                match[1].Value, 
                match[2].Value,
                double.Parse(match[3].Value), 
                double.Parse(match[4].Value),
                double.Parse(match[5].Value),
                double.Parse(match[6].Value),
                double.Parse(match[7].Value),
                double.Parse(match[8].Value),
                match[9].Value
            ));
        }

        if (!result.Any())
            return result;
        
        var averageCost = result.Average(r => r.EstimatedTotalSubtreeCost);
        var sortedList = result.OrderByDescending(r => r.EstimatedTotalSubtreeCost).ToList();
        
        
        return sortedList.Select(r => r with {IsAboveAverageCost = r.EstimatedTotalSubtreeCost >= averageCost}).ToList();
    }
}