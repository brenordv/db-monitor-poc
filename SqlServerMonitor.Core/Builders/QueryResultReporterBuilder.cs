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

    private string ConvertToText(IList<object> list, ReportType type)
    {
        switch (type)
        {
            case ReportType.LongRunningQuery:
                if (!list.Any())
                    return "No long running queries found.";

                var longRunningQueries = list.Cast<LongRunningQueryInfo>().ToList();
                var sb = new StringBuilder();
                sb.AppendLine($"Found {list.Count} long running queries.");

                var queryWithMaxCpuTime = longRunningQueries.OrderByDescending(x => x.CpuTime).First();
                sb.AppendLine(
                    $"Query with max CPU time last ran date {queryWithMaxCpuTime.StartTime}, CPU time {queryWithMaxCpuTime.CpuTime} ms, query text (100 chars): {queryWithMaxCpuTime.Text[..100]}...");

                var queryWithMaxWaitTime = longRunningQueries.OrderByDescending(x => x.WaitTime).First();
                sb.AppendLine(
                    $"Query with max wait time last ran date {queryWithMaxWaitTime.StartTime}, wait time {queryWithMaxWaitTime.WaitTime} ms, query text (100 chars): {queryWithMaxWaitTime.Text[..100]}...");

                var queryWithMaxTotalElapsedTime =
                    longRunningQueries.OrderByDescending(x => x.TotalElapsedTime).First();
                sb.AppendLine(
                    $"Query with max total elapsed time last ran date {queryWithMaxTotalElapsedTime.StartTime}, total elapsed time {queryWithMaxTotalElapsedTime.TotalElapsedTime} ms, query text (100 chars): {queryWithMaxTotalElapsedTime.Text[..100]}...");

                return sb.ToString();

            case ReportType.MissingIndex:
                if (!list.Any())
                    return "No queries with missing indexes found.";

                var missingIndexQueries = list.Cast<MissingIndexInfo>().ToList();
                var sb2 = new StringBuilder();
                sb2.AppendLine($"Found {list.Count} queries with missing indexes.");

                var queryWithMaxImpact = missingIndexQueries.OrderByDescending(x => x.AvgTotalUserCost).First();
                sb2.AppendLine(
                    $"Query with max impact ({queryWithMaxImpact.AvgTotalUserCost}), query text (100 chars): {queryWithMaxImpact.CreateStatement[..100]}...");
                sb2.AppendLine($"Suggested index: {queryWithMaxImpact.CreateStatement}");

                if (missingIndexQueries.Count == 1)
                    return sb2.ToString();

                var queryWithSecondToMaxImpact =
                    missingIndexQueries.OrderByDescending(x => x.AvgTotalUserCost).Skip(1).First();
                sb2.AppendLine(
                    $"Query with second to max impact ({queryWithSecondToMaxImpact.AvgTotalUserCost}), query text (100 chars): {queryWithSecondToMaxImpact.CreateStatement[..100]}...");
                sb2.AppendLine($"Suggested index: {queryWithSecondToMaxImpact.CreateStatement}");

                if (missingIndexQueries.Count == 2)
                    return sb2.ToString();

                var queryWithThirdToMaxImpact =
                    missingIndexQueries.OrderByDescending(x => x.AvgTotalUserCost).Skip(2).First();
                sb2.AppendLine(
                    $"Query with third to max impact ({queryWithThirdToMaxImpact.AvgTotalUserCost}), query text (100 chars): {queryWithThirdToMaxImpact.CreateStatement[..100]}...");
                sb2.AppendLine($"Suggested index: {queryWithThirdToMaxImpact.CreateStatement}");

                return sb2.ToString();

            case ReportType.BadQuery:
                if (!list.Any())
                    return "No bad queries found.";

                var badQueries = list.Cast<BadQueryInfo>().ToList();
                var sb3 = new StringBuilder();
                sb3.AppendLine($"Found {list.Count} bad queries.");

                var mostExecutedQuery = badQueries.OrderByDescending(x => x.ExecutionCount).First();

                var queryWithMaxCpuTime2 = badQueries.OrderByDescending(x => x.AvgCpuTime).First();
                sb3.AppendLine(
                    $"Query with max CPU time ({queryWithMaxCpuTime2.AvgCpuTime}) was executed {queryWithMaxCpuTime2.ExecutionCount} times, taking a total of {queryWithMaxCpuTime2.TotalCpuTime} of the CPU time. , query text (100 chars): {queryWithMaxCpuTime2.SqlText[..100]}...");
                var queryPlan = queryWithMaxCpuTime2.QueryPlan.ExtractStatementsFromPlan();
                sb3.AppendQueryPlan(queryPlan);

                if (mostExecutedQuery == queryWithMaxCpuTime2)
                {
                    sb3.AppendLine("This query is also the most executed query.");
                    return sb3.ToString();
                }

                sb3.AppendLine(
                    $"Most executed query ({mostExecutedQuery.ExecutionCount} times) took a total of {mostExecutedQuery.TotalCpuTime} of the CPU time. , query text (100 chars): {mostExecutedQuery.SqlText[..100]}...");
                queryPlan = mostExecutedQuery.QueryPlan.ExtractStatementsFromPlan();
                sb3.AppendQueryPlan(queryPlan);

                return sb3.ToString();

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public (string reportMessage, IList<DbReportDocument> reportDocuments) Build()
    {
        return (_reportBuilder.ToString(), _reportDocuments);
    }
}