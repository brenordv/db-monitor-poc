using System.Diagnostics;
using System.Text;
using SqlServerMonitor.Core.Models;

namespace SqlServerMonitor.Core.Extensions;

public static class StringBuilderExtensions
{
    
    private static readonly string Line = new ('-', 80);

    public static StringBuilder AppendData<T>(this StringBuilder sb, string name, IEnumerable<T> data) where T : notnull
    {
        Debug.Assert(data != null, nameof(data) + " != null");
        var list = data.ToList();

        if (!list.Any())
        {
            sb.AppendLine($"No {name} found.");
            return sb;
        }
        
        sb.AppendLine($"Found {list.Count} {name}.");
        list.ForEach(x =>
        {
            sb.AppendLine(Line);
            sb.AppendLine(x.ToString());
        });
        sb.AppendLine("\n\n");
        return sb;
    }
    
    public static StringBuilder AppendQueryPlan(this StringBuilder sb, IList<ExecutionPlanStatement> statements)
    {
        if (!statements.Any())
        {
            sb.AppendLine("No query plan found.");
            return sb;
        }
        
        sb.AppendLine("According to the query plan, the worse parts of this query are:");
        foreach (var (text, estimatedCost, isAboveAverageCost) in statements.Where(s => s.IsAboveAverageCost))
        {
            sb.AppendLine($"- {text} (estimated cost: {estimatedCost})");
        }
        sb.AppendLine("\n");
        return sb;
    }
}