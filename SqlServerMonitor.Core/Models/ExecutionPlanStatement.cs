namespace SqlServerMonitor.Core.Models;

public record ExecutionPlanStatement 
(
    string Text,
    double EstimatedCost,
    bool IsAboveAverageCost
);