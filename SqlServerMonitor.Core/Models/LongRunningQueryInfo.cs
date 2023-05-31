namespace SqlServerMonitor.Core.Models;

public record LongRunningQueryInfo
(
    int SessionId,
    DateTime StartTime,
    string Status,
    string Command,
    string WaitType,
    int WaitTime,
    int CpuTime,
    int TotalElapsedTime,
    string Text
);
