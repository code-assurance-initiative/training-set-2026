namespace FleetOps.Domain.Maintenance;

public sealed record MaintenanceAssessment(int CurrentKm, IReadOnlyList<ServiceInterval> DueServices);
