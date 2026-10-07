namespace FleetOps.Contracts.WorkOrders;

public sealed record WorkOrderSummary(
    Guid Id,
    Guid VehicleId,
    string Title,
    string Status,
    IReadOnlyList<WorkOrderLineSummary> Lines,
    decimal? ApprovedTotal,
    DateTimeOffset OpenedAt,
    DateTimeOffset? LastViewedAt);
