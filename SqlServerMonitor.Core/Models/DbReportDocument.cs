using Newtonsoft.Json;
using SqlServerMonitor.Core.Enums;

namespace SqlServerMonitor.Core.Models;

public record DbReportDocument
{
    [JsonProperty("id")]
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public ReportType Type { get; init; }
    public string Text { get; init; }
    public IEnumerable<object> Data { get; init; }
}