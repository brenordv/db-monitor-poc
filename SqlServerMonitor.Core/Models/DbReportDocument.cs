namespace SqlServerMonitor.Core.Models;

public record DbReportDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public IEnumerable<LongRunningQueryInfo> LongRunningQueries { get; init; }
    public IEnumerable<MissingIndexInfo> MissingIndexes { get; init; }
    public IEnumerable<BadQueryInfo> TopBadQueries { get; init; }
}