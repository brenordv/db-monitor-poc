# DB Monitor
The Project Db Monitoring aims to leverage readily available resources like Azure Functions to effectively monitor 
a database. The primary goal is to proactively identify and address issues, such as poor query performance, before they
escalate into full-scale outages. By implementing this monitoring solution, organizations can potentially reduce costs 
associated with downtime and prevent significant database disruptions.

It's worth noting that monitoring alone might not be sufficient to entirely prevent outages. Additional measures like 
performance optimization, query tuning, and infrastructure scalability might be necessary to ensure robust database 
operations.

## What data can you get from this project?

1. Which queries are currently running for a long time.
2. Which queries are missing indexes and the list of missing indexes for each query.
3. Which queries are the worst? And for each query, the top 3 worst operations.
4. Comparative data (current vs previous execution):
   - How many new indexes do I need to create? Are they all form the same table?
   - Do I have a new bad query in the top 10 worst queries (or are they still the same ones from before)?
   - Do any of my bad queries gotten any worst (like more cpu usage)? If so, was it because of a new operation 
     (query plan analysis) or because it was executed more times (executionCount)? 

## What can you do with that info?

1. Save the reports to a CosmosDb collection.
2. Save the reports to a storage file (and maybe use it in an Azure Data Factory pipeline).
3. Send the reports to an email address/Slack/Teams/some other API.
4. Store the reports in a SQL database.


# Pros and cons
1. **Modularity:** Each function is dedicated to a particular task (long running queries, missing indexes, top 10 worst
queries, and combined function). This allows for focused development, maintenance, and scaling.

2. **Serverless Architecture:** Azure Functions is serverless, meaning there is no need to manage the underlying 
infrastructure, allowing you to focus on the code and its functionality.

3. **Scalability:** Azure Functions can scale out automatically based on the workload. This ensures that the performance
management tasks can handle peak loads without manual intervention.

4. **Pay-Per-Use:** Azure Functions follows a consumption plan where you only pay for the time your functions run, 
which can potentially reduce costs.

5. **Event-Driven:** Azure Functions are event-driven, meaning they only run in response to an event or a trigger. 
This ensures resources are used efficiently.

Cons of using Azure Functions for SQL Server performance management:

1. **Cold Start:** Azure Functions could have a delay in their initial execution, known as a cold start. This could 
impact the timely execution of performance management tasks. (Might just apply to the Long Running Query.)

2. **Complexity:** You'll be relying on a whole new infrastructure and codebase to monitor the database and some of this
could be done by using native Azure tools.

3. **Long-Running Tasks:** Azure Functions have a maximum execution time (by default, it's 5 minutes on a Consumption 
plan but can be extended to 10 minutes or unlimited in a Premium plan). Functions handling long-running queries may hit
this limit. The current queries are optimized, so in the current form, this should not be a problem.

4. **Security:** Sensitive information (like connection strings to your SQL server) needs to be securely stored and 
managed. Mismanagement could lead to security vulnerabilities.

## Alternatives native to Azure for SQL Server Performance Management

1. **[Azure SQL Database Advisor](https://learn.microsoft.com/en-us/azure/azure-sql/database/database-advisor-implement-performance-recommendations?view=azuresql):** This service provides recommendations for creating indexes, dropping unused indexes,
and parameterizing queries to optimize performance. 

2. **[Query Performance Insight](https://docs.microsoft.com/en-us/azure/azure-sql/database/query-performance-insight-use):** It provides insights into your database's resource consumption patterns and
identifies long-running queries that may be affecting performance.

3. **[Azure Monitor](https://docs.microsoft.com/en-us/azure/azure-monitor/overview):** You can use this tool to monitor SQL Server performance in real time, including tracking 
SQL Server metrics and setting up alerts for when certain conditions are met.

4. **[Azure Log Analytics](https://docs.microsoft.com/en-us/azure/azure-monitor/logs/data-platform-logs):** Collects and analyzes logs for insight into database activity and performance.

5. **[Azure SQL Database Automatic Tuning](https://learn.microsoft.com/en-us/azure/azure-sql/database/automatic-tuning-overview?view=azuresql):** It's a fully managed intelligent performance service that uses built-in 
intelligence to continuously monitor queries executed on a database, and it automatically improves their performance.


# Project parts
This project consists of a Azure Function App with 4 functions.

## `FnLongRunningQueries` - Find long running queries
This query retrieves information about currently executing requests in SQL Server.

Below is the SQL query that retrieves information about currently executing requests in SQL Server that are neither 
background nor sleeping, exclude the current session, and have a total elapsed time greater than a specified maximum 
time.

```sql
SELECT 
    r.session_id AS SessionId,
    r.start_time AS StartTime,
    r.status AS Status,
    r.command AS Command,
    r.wait_type AS WaitType,
    r.wait_time AS WaitTime,
    r.cpu_time AS CpuTime,
    r.total_elapsed_time AS TotalElapsedTime,
    t.text AS Text 
FROM 
    sys.dm_exec_requests r 
CROSS APPLY 
    sys.dm_exec_sql_text(r.sql_handle) t 
WHERE 
    r.status NOT IN ('background', 'sleeping') 
    AND r.session_id <> @@SPID 
    AND r.total_elapsed_time > @maxTime;
```

Query fields:
- `SessionId`: The ID of the session that is executing the request.
- `StartTime`: The time when the request started.
- `Status`: The status of the request (running, suspended, sleeping, etc.).
- `Command`: The command that is being executed.
- `WaitType`: If the request is waiting, this is the type of the wait.
- `WaitTime`: The total time, in milliseconds, that this request has been waiting.
- `CpuTime`: The time that this request has been running on the CPU. This does not include time spent waiting.
- `TotalElapsedTime`: The total time, in milliseconds, that this request has been running.
- `Text`: The text of the SQL batch that is being executed.

This query retrieves information about currently executing requests in SQL Server from the `sys.dm_exec_requests` 
dynamic management view. The `CROSS APPLY` operator is used to join this view with the `sys.dm_exec_sql_text()` 
function, which returns the text of the SQL batch for each request. The query filters out requests that are background 
tasks, sleeping tasks, the current session, and those with a total elapsed time not exceeding a specified maximum time.

Where fields:
- `@maxTime` - The maximum total elapsed time, in milliseconds. Any query with a total elapsed time greater than this 
will be included.
- `@@SPID` - The ID of the current session. This is used to exclude the current session from the result set.

As with other diagnostic queries, be mindful of potential performance impacts when running this query on a
production system.

## `FnMissingIndexes` - Detect missing indexes
This function retrieves information about missing indexes in the SQL Server.

Below is the SQL query that retrieves that information:
```sql
SELECT 
    d.[database_id] AS DatabaseId,
    d.[object_id] AS ObjectId,
    d.[statement] AS FullyQualifiedTableName,
    ISNULL(d.[equality_columns], '') AS EqualityColumns,
    ISNULL(d.[inequality_columns], '') AS InequalityColumns,
    ISNULL(d.[included_columns], '') AS IncludedColumns,
    s.avg_total_user_cost AS AvgTotalUserCost,
    d.[statement] + 
    ' WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, 
    IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, 
    ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]' AS CreateStatement 
FROM 
    sys.dm_db_missing_index_details d 
INNER JOIN 
    sys.dm_db_missing_index_groups g ON d.index_handle = g.index_handle 
INNER JOIN 
    sys.dm_db_missing_index_group_stats s ON g.index_group_handle = s.group_handle;
```

Query Fields:
- `DatabaseId`: The ID of the database where the table with the missing index is located.
- `ObjectId`: The ID of the table where the index is missing.
- `FullyQualifiedTableName`: The full name of the table (including schema name) where the index is missing.
- `EqualityColumns`: Comma-separated list of columns that are used in equality predicates, like `WHERE column = value`.
- `InequalityColumns`: Comma-separated list of columns that are used in inequality predicates, like `WHERE column < value`.
- `IncludedColumns`: Comma-separated list of columns to be included in the missing index.
- `AvgTotalUserCost`: Average cost of the user queries that could be reduced with the missing index.
- `CreateStatement`: A generated script that can be used to create the missing index.

This query joins three dynamic management views (`sys.dm_db_missing_index_details`, `sys.dm_db_missing_index_groups`, 
`sys.dm_db_missing_index_group_stats`) to retrieve information about missing indexes in the database. It provides 
details about the missing indexes and a script to create each of them.

Please note that these views suggest what indexes are missing based on the executed queries since the last SQL Server 
restart or the last time the database was brought online. Use this information wisely because it's always recommended 
to review and test suggested indexes before implementing them in a production environment.

## `FnTopBadQueries` - Find top 10 bad queries
This function retrieves information about the top 10 queries with the highest average CPU time in SQL Server.
Query used:
```sql
SELECT TOP 10 
    qs.total_worker_time AS TotalCpuTime, 
    qs.execution_count AS ExecutionCount, 
    qs.total_worker_time/qs.execution_count AS AvgCpuTime, 
    t.text AS SqlText, 
    qp.query_plan AS QueryPlan 
FROM 
    sys.dm_exec_query_stats qs 
CROSS APPLY 
    sys.dm_exec_sql_text(qs.sql_handle) t 
CROSS APPLY 
    sys.dm_exec_query_plan(qs.plan_handle) qp 
ORDER BY 
    qs.total_worker_time DESC;
```

Query fields:
- `TotalCpuTime`: The total worker time (in CPU time, measured in microseconds) used by the query.
- `ExecutionCount`: The number of times that the query plan has been executed since it was last compiled.
- `AvgCpuTime`: The average CPU time per execution. This is calculated by dividing the total CPU time by the execution count.
- `SqlText`: The text of the SQL batch that contains the statement.
- `QueryPlan`: The Showplan XML for the query plan.

This query uses the `sys.dm_exec_query_stats` dynamic management view, which returns aggregate performance statistics 
for cached query plans in SQL Server. The view is joined with two functions, `sys.dm_exec_sql_text()` and
`sys.dm_exec_query_plan()`, to return the SQL text and the query plan for each query, respectively. The `CROSS APPLY` 
operator is used to join the view with these functions.

The query returns the 10 queries with the highest total CPU time, which can help identify queries that are consuming 
significant CPU resources. Please note that these statistics are based on cached query plans, and the data resets when 
the SQL Server service is restarted or when the plans are removed from the cache.

## `FnAllInOne` - All in one function
This functions does what the previous functions do, but in one function. 


# Function Reports
## `FnLongRunningQueries` - Find long running queries
Each `LongRunningQueryInfo` object contains details about a long-running SQL query, such as its start time, CPU time, 
wait time, total elapsed time, and the SQL text of the query. The method identifies and includes details of the queries
with maximum CPU time, wait time, and total elapsed time in the report.

## `FnMissingIndexes` - Detect missing indexes
Each `MissingIndexInfo` object contains information about a SQL query that could potentially benefit from having an 
additional index. It includes the SQL text of the query and the statement to create the missing index. The method 
identifies and includes details of the top 3 queries with maximum average total user cost in the report.

## `FnTopBadQueries` - Find top 10 bad queries
Each `BadQueryInfo` object contains details about a SQL query that has been identified as "bad" due to performance 
issues. The method identifies and includes details of the query with maximum average CPU time and the most frequently 
executed query in the report.

This report also tries to extract data from the Query Plan XML to identify the most expensive operators in the query.
It uses a regular expression (regex) to extract important attributes from each RelOp node in the XML document.

Here's the attributes extracted and how they can help optimize SQL Server performance:
1. `PhysicalOp`: This attribute provides the physical operation that SQL Server's query optimizer selected to implement
the logical operation. By analyzing the PhysicalOp, we can understand the actual strategies used by the SQL Server to 
execute the query.

2. `LogicalOp`: This is the logical operation that SQL Server's query optimizer has chosen for a specific part of the 
query. If the logical operation seems sub-optimal, it might be an indication that the query or the database schema can 
be optimized.

3. `EstimateRows`: This is an estimate of the number of rows the operation will process. If this estimate is 
significantly different from the actual number of rows processed, it could mean that the statistics used by the query 
optimizer are outdated, leading to sub-optimal execution plans.

4. `EstimatedRowsRead`: This attribute tells how many rows are expected to be read to satisfy the query. If the 
EstimatedRowsRead is excessively large, it could point to an operation that is reading more data than necessary, 
which could be a sign of a missing index or a less-than-optimal query.

5. `EstimateIO`: This is an estimate of the IO cost for the operation. A high IO cost could indicate a need for query 
optimization or additional indexes.

6. `EstimateCPU`: This is an estimate of the CPU cost for the operation. A high CPU cost could indicate a 
computationally complex operation and might suggest areas where the query could be optimized.

7. `AvgRowSize`: This is the average size of a row for the operation's output. A larger row size may lead to more IO 
and memory usage.

8. `EstimatedTotalSubtreeCost`: This is a measure of the total cost of the operation, including all its sub-operations.
This measure is crucial for identifying the most resource-intensive parts of the query.

9. `EstimatedExecutionMode`: This attribute tells whether the operation is expected to be performed in Row or Batch 
mode. Batch mode is typically more efficient for large data sets, so if an operation that processes a lot of data 
is in Row mode, there might be an opportunity for optimization.

By analyzing these attributes, a DBA or developer can identify potential bottlenecks and areas for improvement in 
SQL Server queries.

## `FnAllInOne` - All in one function
As you can imagine, the `FnAllInOne` function does what the previous functions do, but in one function.

## Report Document
Along with the report described previously, an instance of a CosmosDb (compatible) object is created with the complete
data extracted. This object can be used to do a more in-depth analysis of server's performance.


# About Azure Functions
## Lifecycle
The Azure Functions runtime invokes input bindings after the function trigger fires, but before the function code runs.

Here's a brief overview of the Azure Function execution lifecycle:

1. **Trigger fires**: An event occurs that matches the conditions specified in your function's trigger binding. This 
could be an HTTP request, a message arriving in a queue, a timer going off, etc.

2. **Input bindings execute**: The Azure Functions runtime gathers the data necessary for the function to execute. 
It does this by executing any input bindings you've specified. These input bindings collect data from other services 
(like databases or storage accounts), and provide them as inputs to your function code.

3. **Function executes**: Your function code runs, using the data provided by the input bindings. You can use multiple
input bindings, and the data from all of them is available when your function starts.

4. **Output bindings execute**: After your function finishes executing, any output bindings you've specified are 
executed. These output bindings take the results of your function and send them to other services (like databases, 
queues, or HTTP responses).

5. **Function completes**: The function execution lifecycle is complete. The runtime handles any necessary cleanup,
like closing database connections.

## References
Below are the primary sources of information that can provide more details about Azure Function Bindings and the 
execution lifecycle.

1. **[Azure Functions triggers and bindings concepts](https://docs.microsoft.com/en-us/azure/azure-functions/functions-triggers-bindings)**:
   This is a general guide to the concepts of triggers and bindings in Azure Functions.

2. **[Azure Functions developer guide](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference)**:
   This guide provides information on the lifecycle of an Azure Function, among other topics.

3. **[Work with Azure Functions Proxies](https://docs.microsoft.com/en-us/azure/azure-functions/functions-proxies)**:
   This guide includes a section on the execution order of bindings and proxies.


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


# Refreshers
## SQL Server - CROSS APPLY
The `CROSS APPLY` operator in SQL Server allows you to invoke a table-valued function for each row returned by an outer
table expression of a query. `CROSS APPLY` works like the `JOIN` command and also allows you to join a table to a
function that returns a table.

The main feature of `CROSS APPLY` is that it allows referencing columns of the outer table from within the function. 
This is something you cannot do with a regular JOIN.

Let's take a simple example. Suppose we have a table `Products` and a table-valued function `GetProductSales`, which
takes a `ProductID` as parameter and returns the sales for the given product.

You could use `CROSS APPLY` to join the `Products` table and the `GetProductSales` function like this:

```sql
SELECT 
    Products.ProductName, 
    ProductSales.SaleDate, 
    ProductSales.SaleAmount 
FROM 
    Products 
CROSS APPLY 
    GetProductSales(Products.ProductID) AS ProductSales;
```

In this query, for each row in the `Products` table, the `GetProductSales` function is invoked with the `ProductID`
from that row. The `CROSS APPLY` operator then matches the rows from `Products` and `GetProductSales` together, much 
like a JOIN.

Here are a couple of key points to understand about `CROSS APPLY`:

- `CROSS APPLY` only returns rows from the outer table expression where the table-valued function returns a result set.
If the function returns an empty set for a row in the outer table, that row is not included in the result set. If you
want to include those rows, you should use `OUTER APPLY` instead, which works similar to a `LEFT OUTER JOIN`.
- `CROSS APPLY` can also be used with a subquery on the right side, not just with a table-valued function.
- `CROSS APPLY` is generally useful with functions that take columns from the outer table as arguments.
- In some cases, `CROSS APPLY` can be used as an alternative to complex joins, providing better readability and
potentially better performance.

References:

- Microsoft's official documentation on the `APPLY` clause, which includes `CROSS APPLY` 
and `OUTER APPLY`: [APPLY (Transact-SQL) - SQL Server | Microsoft Docs](https://docs.microsoft.com/en-us/sql/t-sql/queries/from-transact-sql?view=sql-server-ver15#apply)

Remember to consider the version of SQL Server you are using when reading any material about SQL Server features, 
as certain features or behaviors might change between different versions.

### Still don't get it?
Imagine you have a cookbook with two sections. The first section is a list of recipes, each with its name, cuisine type,
and preparation time. The second section is a more detailed one; for each recipe, it lists all the ingredients needed.

Now, suppose you want to create a complete list that shows each recipe with its details from the first section, along
with all the individual ingredients required for that recipe from the second section.

In terms of SQL Server, the first section of your cookbook is like a table (let's call it `Recipes`), and the second 
section is like a table-valued function (let's call it `GetIngredients`). This function, when given a recipe name, 
returns a table with all the ingredients for that recipe.

To build the complete list, you need to go through each recipe from the `Recipes` table (the outer table), invoke the 
`GetIngredients` function to get all ingredients for the current recipe (like invoking a function for each row in the
outer table), and then list the recipe together with its ingredients. This operation is analogous to `CROSS APPLY`.

So, with `CROSS APPLY`, you can "apply" the `GetIngredients` function to each recipe in the `Recipes` table and get a 
combined result that's like a detailed cookbook, where each recipe is directly associated with its individual 
ingredients.

This is, of course, a simplification, and actual SQL Server operations are more complex. But hopefully, this gives you
a general understanding of how `CROSS APPLY` works.

## SQL Server - Query Plan - RelOp
In SQL Server's query execution plan XML, the `RelOp` tag refers to a relational operation or a logical operation. 
These operations represent a single step in the execution of the SQL query. The XML execution plan provides a way for 
SQL Server to communicate how it broke down the SQL query into these basic operations.

For instance, a SQL query might be broken down into a tree of operations like filtering, joining, sorting, and 
aggregating.

In the XML plan, each `RelOp` tag will have a number of attributes and possibly child tags that describe what the 
operation does. The main attributes you'll often see include:

- `NodeId`: A unique identifier for this operation within the execution plan.
- `PhysicalOp`: The physical operator SQL Server uses to perform this step. This could be things like a table scan, an 
index seek, a hash match, and so forth.
- `LogicalOp`: The logical operation this step is performing. This is a more abstract description of what the operation 
does, like "Join" or "Filter".
- `EstimateRows`: The estimated number of rows that this operation will return.
- `EstimateIO`, `EstimateCPU`: The estimated IO and CPU costs for this operation. The units aren't directly meaningful,
but they're useful for comparing the cost of different parts of the plan.
- `Parallel`: This attribute tells you if the operation can be executed in parallel.

Child tags can include `OutputList` (the columns that are output by this operation), `Predicate` (the filter condition
for a Filter or Join operation), `IndexScan` or `TableScan` details, and so on.

Understanding the `RelOp` tags, along with their child tags, in the XML query plan can help you understand how 
SQL Server is executing your SQL query and can help you identify possible performance issues.

Remember that analyzing query plans is a complex task that requires knowledge about SQL Server internals, database 
schema, indices, and the specific query being executed.