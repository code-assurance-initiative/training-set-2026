namespace FleetOps.Contracts.Inspections;

public sealed record InspectionSummary(
    Guid Id,
    Guid VehicleId,
    DateTimeOffset InspectedAt,
    bool Passed,
    IReadOnlyList<string> Defects,
    Guid? FollowUpWorkOrderId);
