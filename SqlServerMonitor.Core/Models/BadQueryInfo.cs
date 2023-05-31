namespace SqlServerMonitor.Core.Models;

public record BadQueryInfo
(
    long TotalCpuTime,
    int ExecutionCount,
    long AvgCpuTime,
    string SqlText,
    string QueryPlan
);
