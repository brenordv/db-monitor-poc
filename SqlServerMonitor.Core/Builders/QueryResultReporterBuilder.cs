using System.Text;
using SqlServerMonitor.Core.Enums;
using SqlServerMonitor.Core.Extensions;
using SqlServerMonitor.Core.Models;

namespace SqlServerMonitor.Core.Builders;

public class QueryResultReporterBuilder
{
    private readonly StringBuilder _reportBuilder = new();
    private readonly IList<DbReportDocument> _reportDocuments = new List<DbReportDocument>();

    public QueryResultReporterBuilder Add(IEnumerable<object> data, ReportType type, string name)
    {
        var list = data.ToList();
        _reportDocuments.Add(new DbReportDocument
        {
            Type = type,
            Data = list,
            Text = ConvertToText(list, type)
        });
        _reportBuilder.AppendData(name, list);

        return this;
    }

    private static string ConvertToText<T>(ICollection<T> list, ReportType type) where T : class
    {
        if (!list.Any())
        {
            return type switch
            {
                ReportType.LongRunningQuery => "No long running queries found.",
                ReportType.MissingIndex => "No queries with missing indexes found.",
                ReportType.BadQuery => "No bad queries found.",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        var sb = new StringBuilder();

        switch (type)
        {
            case ReportType.LongRunningQuery:
                var longRunningQueries = list.Cast<LongRunningQueryInfo>().ToList();
                sb.AppendLine($"Found {longRunningQueries.Count} long running queries.");
                sb.AppendDetailToReport(longRunningQueries, x => x.CpuTime, "Total CPU time");
                sb.AppendDetailToReport(longRunningQueries, x => x.WaitTime, "Total Wait time");
                sb.AppendDetailToReport(longRunningQueries, x => x.TotalElapsedTime, "Total Elapsed time");
                break;

            case ReportType.MissingIndex:
                var missingIndexQueries = list.Cast<MissingIndexInfo>().ToList();
                sb.AppendLine($"Found {missingIndexQueries.Count} queries with missing indexes.");
                sb.AppendDetailToReport(missingIndexQueries, x => x.AvgTotalUserCost, "Avg Total User Cost");
                break;

            case ReportType.BadQuery:
                var badQueries = list.Cast<BadQueryInfo>().ToList();
                sb.AppendLine($"Found {badQueries.Count} top bad queries.");
                sb.AppendDetailToReport(badQueries, x => x.ExecutionCount, "Execution Count");
                sb.AppendDetailToReport(badQueries, x => x.TotalCpuTime, "Total CPU time");
                
                var mostExecutedQuery = badQueries.OrderByDescending(x => x.ExecutionCount).First();
                var mostCpuTimeQuery = badQueries.OrderByDescending(x => x.TotalCpuTime).First();
                if (mostExecutedQuery == mostCpuTimeQuery) break;
                
                sb.AppendLine("Note: The most executed query in the top 10 bad queries is not the one with the most CPU time.");
                AppendQueryInfo(sb, mostExecutedQuery, "Most executed query");
                AppendQueryInfo(sb, mostCpuTimeQuery, "Most Cpu time query");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return sb.ToString();
    }
    
    private static void AppendQueryInfo<T>(StringBuilder sb, T query, string prefix) where T : class
    {
        switch (query)
        {
            case LongRunningQueryInfo longRunningQuery:
                sb.AppendLine(
                    $"{prefix} last ran date {longRunningQuery.StartTime}, CPU time {longRunningQuery.CpuTime} ms, wait time {longRunningQuery.WaitTime} ms, total elapsed time {longRunningQuery.TotalElapsedTime} ms, query text (100 chars): {longRunningQuery.Text[..Math.Min(longRunningQuery.Text.Length, 100)]}...");
                break;

            case MissingIndexInfo missingIndexQuery:
                sb.AppendLine(
                    $"{prefix} ({missingIndexQuery.AvgTotalUserCost}), query text (100 chars): {missingIndexQuery.CreateStatement[..Math.Min(missingIndexQuery.CreateStatement.Length, 100)]}...");
                sb.AppendLine($"Suggested index: {missingIndexQuery.CreateStatement}");
                break;

            case BadQueryInfo badQuery:
                sb.AppendLine(
                    $"{prefix} was executed {badQuery.ExecutionCount} times with an avg CPU time of {badQuery.AvgCpuTime}, taking a total of {badQuery.TotalCpuTime} of the CPU time. , query text (100 chars): {badQuery.SqlText[..Math.Min(badQuery.SqlText.Length, 100)]}...");
                
                var queryPlan = badQuery.QueryPlan.ExtractStatementsFromPlan();
                sb.AppendQueryPlan(queryPlan);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(query), query, "Unexpected query type");
        }
    }

    public (string reportMessage, IList<DbReportDocument> reportDocuments) Build()
    {
        return (_reportBuilder.ToString(), _reportDocuments);
    }
}