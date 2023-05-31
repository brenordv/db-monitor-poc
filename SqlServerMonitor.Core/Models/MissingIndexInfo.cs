namespace SqlServerMonitor.Core.Models;

public record MissingIndexInfo
(
    int DatabaseId,
    int ObjectId,
    string FullyQualifiedTableName,
    string EqualityColumns,
    string InequalityColumns,
    string IncludedColumns,
    string CreateStatement
);
