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
    
    public static StringBuilder AppendQueryPlan(this StringBuilder sb, IList<RelOp> statements)
    {
        if (!statements.Any())
        {
            sb.AppendLine("No query plan found.");
            return sb;
        }
        
        sb.AppendLine("According to the query plan, the worse parts of this query are:");
        foreach (var relOp in statements.Where(s => s.IsAboveAverageCost))
        {
            sb.AppendLine($"- PhysicalOp: {relOp.PhysicalOp} / LogicalOp: {relOp.LogicalOp} / Cost: {relOp.EstimatedTotalSubtreeCost} / Exec Mode: {relOp.EstimatedExecutionMode} / Estimated Rows Read {relOp.EstimateRowsRead} / Estimated Rows: {relOp.EstimateRows} (avg size: {relOp.AvgRowSize} / Estimated IO: {relOp.EstimateIo} / Estimated CPU: {relOp.EstimateCpu})");
        }
        sb.AppendLine("\n");
        return sb;
    }
}