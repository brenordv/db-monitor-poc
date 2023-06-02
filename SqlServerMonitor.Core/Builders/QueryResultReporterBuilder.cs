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

    private static string ConvertToText<T>(IList<T> list, ReportType type) where T : class
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
        sb.AppendLine($"Found {list.Count} {type.ToString().ToLowerInvariant()}s.");

        switch (type)
        {
            case ReportType.LongRunningQuery:
                var longRunningQueries = list.Cast<LongRunningQueryInfo>().OrderByDescending(x => x.CpuTime).ToList();
                AppendTopQueries(sb, longRunningQueries, x => x.CpuTime, x => x.WaitTime, x => x.TotalElapsedTime);
                break;

            case ReportType.MissingIndex:
                var missingIndexQueries =
                    list.Cast<MissingIndexInfo>().OrderByDescending(x => x.AvgTotalUserCost).ToList();
                AppendTopQueries(sb, missingIndexQueries, x => x.AvgTotalUserCost);
                break;

            case ReportType.BadQuery:
                var badQueries = list.Cast<BadQueryInfo>().OrderByDescending(x => x.AvgCpuTime).ToList();
                AppendTopQueries(sb, badQueries, x => x.AvgCpuTime);
                var mostExecutedQuery = badQueries.OrderByDescending(x => x.ExecutionCount).First();
                if (mostExecutedQuery != badQueries.First())
                {
                    AppendQueryInfo(sb, mostExecutedQuery, "Most executed query");
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return sb.ToString();
    }

    private static void AppendTopQueries<T>(StringBuilder sb, List<T> sortedQueries, params Func<T, object>[] orderByProps)
        where T : class
    {
        for (var i = 0; i < Math.Min(3, sortedQueries.Count); i++)
        {
            var query = sortedQueries[i];
            var propName = orderByProps[i].Method.Name.Replace("get_", "");
            AppendQueryInfo(sb, query, $"Query with {i + 1}-th max {propName}");
        }
    }

    private static void AppendQueryInfo<T>(StringBuilder sb, T query, string prefix) where T : class
    {
        switch (query)
        {
            case LongRunningQueryInfo longRunningQuery:
                sb.AppendLine(
                    $"{prefix} last ran date {longRunningQuery.StartTime}, CPU time {longRunningQuery.CpuTime} ms, wait time {longRunningQuery.WaitTime} ms, total elapsed time {longRunningQuery.TotalElapsedTime} ms, query text (100 chars): {longRunningQuery.Text.Substring(0, Math.Min(longRunningQuery.Text.Length, 100))}...");
                break;

            case MissingIndexInfo missingIndexQuery:
                sb.AppendLine(
                    $"{prefix} ({missingIndexQuery.AvgTotalUserCost}), query text (100 chars): {missingIndexQuery.CreateStatement.Substring(0, Math.Min(missingIndexQuery.CreateStatement.Length, 100))}...");
                sb.AppendLine($"Suggested index: {missingIndexQuery.CreateStatement}");
                break;

            case BadQueryInfo badQuery:
                sb.AppendLine(
                    $"{prefix} ({badQuery.AvgCpuTime}) was executed {badQuery.ExecutionCount} times, taking a total of {badQuery.TotalCpuTime} of the CPU time. , query text (100 chars): {badQuery.SqlText.Substring(0, Math.Min(badQuery.SqlText.Length, 100))}...");
                
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