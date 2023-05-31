# DB Monitor
TBD. This is still a WIP.

## Installation
Required environment variables:
```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "SqlConnectionString": "your connection string here",
        "MaxQueryTimeInMs": 1000,
        "LongRunningQuery": "SELECT r.session_id as SessionId, r.start_time as StartTime, r.status as Status, r.command as Command, r.wait_type as WaitType, r.wait_time as WaitTime, r.cpu_time as CpuTime, r.total_elapsed_time as TotalElapsedTime, t.text as Text FROM sys.dm_exec_requests r CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t WHERE r.status NOT IN ('background', 'sleeping') AND r.session_id <> @@SPID AND r.total_elapsed_time > @maxTime",
        "MissingIndexesQuery": "SELECT d.[database_id] as DatabaseId, d.[object_id] as ObjectId, d.[statement] AS FullyQualifiedTableName, ISNULL(d.[equality_columns],'') AS EqualityColumns, ISNULL(d.[inequality_columns],'') AS InequalityColumns, ISNULL(d.[included_columns],'') AS IncludedColumns, d.[statement] + ' WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]' AS CreateStatement FROM sys.dm_db_missing_index_details d INNER JOIN sys.dm_db_missing_index_groups AS g ON d.index_handle = g.index_handle",
        "TopBadQueriesQuery": "SELECT TOP 10 qs.total_worker_time AS TotalCpuTime, qs.execution_count AS ExecutionCount, qs.total_worker_time/qs.execution_count AS AvgCpuTime, t.text AS SqlText, qp.query_plan AS QueryPlan FROM sys.dm_exec_query_stats qs CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) t CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp ORDER BY qs.total_worker_time DESC",
        "OutDatabaseName": "master",
        "OutCollectionName": "DbMonitorCollection",
        "CosmosDbConnectionString": "add connection string here"
    }
}
```