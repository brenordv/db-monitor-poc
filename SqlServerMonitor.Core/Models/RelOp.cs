namespace SqlServerMonitor.Core.Models;

public record RelOp 
(
    string PhysicalOp,
    string LogicalOp,
    double EstimateRows,
    double EstimateRowsRead,
    double EstimateIo,
    double EstimateCpu,
    double AvgRowSize,
    double EstimatedTotalSubtreeCost,
    string EstimatedExecutionMode,
    bool IsAboveAverageCost = false
);