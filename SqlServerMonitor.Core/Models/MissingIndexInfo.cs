namespace SqlServerMonitor.Core.Models;

public record MissingIndexInfo
(
    int DatabaseId,
    int ObjectId,
    string FullyQualifiedTableName,
    string EqualityColumns,
    string InequalityColumns,
    string IncludedColumns,
    double AvgTotalUserCost,
    
    /// <summary>
    /// The CREATE INDEX statement that can be used to create the missing index.
    /// </summary>
    string CreateStatement
);
