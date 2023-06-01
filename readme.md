# DB Monitor
TBD. This is still a WIP.


# About Azure Functions
## Lifecycle
The Azure Functions runtime invokes input bindings after the function trigger fires, but before the function code runs.

Here's a brief overview of the Azure Function execution lifecycle:

1. **Trigger fires**: An event occurs that matches the conditions specified in your function's trigger binding. This could be an HTTP request, a message arriving in a queue, a timer going off, etc.

2. **Input bindings execute**: The Azure Functions runtime gathers the data necessary for the function to execute. It does this by executing any input bindings you've specified. These input bindings collect data from other services (like databases or storage accounts), and provide them as inputs to your function code.

3. **Function executes**: Your function code runs, using the data provided by the input bindings. You can use multiple input bindings, and the data from all of them is available when your function starts.

4. **Output bindings execute**: After your function finishes executing, any output bindings you've specified are executed. These output bindings take the results of your function and send them to other services (like databases, queues, or HTTP responses).

5. **Function completes**: The function execution lifecycle is complete. The runtime handles any necessary cleanup, like closing database connections.

## References
Below are the primary sources of information that can provide more details about Azure Function Bindings and the execution lifecycle.

1. **Azure Functions triggers and bindings concepts**:
   This is a general guide to the concepts of triggers and bindings in Azure Functions.
   [Link to Documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-triggers-bindings)

2. **Azure Functions developer guide**:
   This guide provides information on the lifecycle of an Azure Function, among other topics.
   [Link to Documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference)

3. **Work with Azure Functions Proxies**:
   This guide includes a section on the execution order of bindings and proxies.
   [Link to Documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-proxies)

Please note that Microsoft documentation may update over time and some specifics can change, especially since Azure and its services are continuously evolving. Make sure to check the latest official documentation for the most current and accurate information.


# Installation
## Required Azure resources
1. Azure Function App
2. Azure SQL Database
3. Azure Cosmos DB
4. Azure Storage Account
5. Azure Key Vault (Optional, but highly recommended)

## Required environment variables
```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "SqlConnectionString": "your connection string here",
        "MaxQueryTimeInMs": 1000,
        "LongRunningQuery": "SELECT r.session_id as SessionId, r.start_time as StartTime, r.status as Status, r.command as Command, r.wait_type as WaitType, r.wait_time as WaitTime, r.cpu_time as CpuTime, r.total_elapsed_time as TotalElapsedTime, t.text as Text FROM sys.dm_exec_requests r CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t WHERE r.status NOT IN ('background', 'sleeping') AND r.session_id <> @@SPID AND r.total_elapsed_time > @maxTime",
        "MissingIndexesQuery": "SELECT d.[database_id] as DatabaseId, d.[object_id] as ObjectId, d.[statement] AS FullyQualifiedTableName, ISNULL(d.[equality_columns],'') AS EqualityColumns, ISNULL(d.[inequality_columns],'') AS InequalityColumns, ISNULL(d.[included_columns],'') AS IncludedColumns, s.avg_total_user_cost AS AvgTotalUserCost, d.[statement] + ' WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]' AS CreateStatement FROM sys.dm_db_missing_index_details d INNER JOIN sys.dm_db_missing_index_groups g ON d.index_handle = g.index_handle INNER JOIN sys.dm_db_missing_index_group_stats s ON g.index_group_handle = s.group_handle",
        "TopBadQueriesQuery": "SELECT TOP 10 qs.total_worker_time AS TotalCpuTime, qs.execution_count AS ExecutionCount, qs.total_worker_time/qs.execution_count AS AvgCpuTime, t.text AS SqlText, qp.query_plan AS QueryPlan FROM sys.dm_exec_query_stats qs CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) t CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp ORDER BY qs.total_worker_time DESC",
        "OutDatabaseName": "master",
        "OutCollectionName": "DbMonitorCollection",
        "CosmosDbConnectionString": "add connection string here"
    }
}
```

## Instructions
TBD
