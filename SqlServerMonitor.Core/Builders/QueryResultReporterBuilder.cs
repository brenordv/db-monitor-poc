using System.Diagnostics;
using System.Text;
using SqlServerMonitor.Core.Extensions;
using SqlServerMonitor.Core.Models;

namespace SqlServerMonitor.Core.Build;

public class QueryResultReporterBuilder
{
    private readonly StringBuilder _reportBuilder = new();
    private IList<LongRunningQueryInfo> _longRunningQueries;
    private IList<MissingIndexInfo> _missingIndexData;
    private IList<BadQueryInfo> _topBadQueries;
    
    public QueryResultReporterBuilder AddData(IEnumerable<LongRunningQueryInfo> longRunningQueries)
    {
        Debug.Assert(longRunningQueries != null, nameof(longRunningQueries) + " != null");
        _longRunningQueries = longRunningQueries.ToList();
        _reportBuilder.AppendData("long running queries", _longRunningQueries);
        return this;
    }

    public QueryResultReporterBuilder AddData(IEnumerable<MissingIndexInfo> missingIndexData)
    {
        Debug.Assert(missingIndexData != null, nameof(missingIndexData) + " != null");
        _missingIndexData = missingIndexData.ToList();
        _reportBuilder.AppendData("queries with missing indexes", _missingIndexData);
        return this;
    }
    
    public QueryResultReporterBuilder AddData(IEnumerable<BadQueryInfo> topBadQueries)
    {
        Debug.Assert(topBadQueries != null, nameof(topBadQueries) + " != null");
        _topBadQueries = topBadQueries.ToList();
        _reportBuilder.AppendData("top bad queries", _topBadQueries);
        return this;
    }
    
    public (string reportMessage, DbReportDocument reportDocument) Build()
    {
        return (_reportBuilder.ToString(), new DbReportDocument
        {
            LongRunningQueries = _longRunningQueries,
            MissingIndexes = _missingIndexData,
            TopBadQueries = _topBadQueries
        });
    }
}