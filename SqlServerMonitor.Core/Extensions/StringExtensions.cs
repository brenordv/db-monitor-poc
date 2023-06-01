using System.Text.RegularExpressions;
using SqlServerMonitor.Core.Models;

namespace SqlServerMonitor.Core.Extensions;

public static class StringExtensions
{
    public static IList<ExecutionPlanStatement> ExtractStatementsFromPlan(this string queryPlan)
    {
        var result = new List<ExecutionPlanStatement>();
        if (string.IsNullOrWhiteSpace(queryPlan))
            return result;
        
        var matches = Regex.Matches(queryPlan, "<StmtSimple .*?StatementText=\"(.*?)\".*?EstimatedSubtreeCost=\"(.*?)\".*?</StmtSimple>");

        foreach (var match in matches.Select(m => m.Groups))
        {
            var statementText = match[1].Value;
            if (!double.TryParse(match[2].Value, out var estimatedCost)) continue;
            result.Add(new ExecutionPlanStatement(statementText, estimatedCost, false));
        }
        
        var averageCost = result.Average(r => r.EstimatedCost);
        var sortedList = result.OrderByDescending(r => r.EstimatedCost).ToList();
        
        
        return sortedList.Select(r => r with {IsAboveAverageCost = r.EstimatedCost >= averageCost}).ToList();
    }
}