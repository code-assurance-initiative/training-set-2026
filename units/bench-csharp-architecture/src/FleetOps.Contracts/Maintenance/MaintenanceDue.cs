namespace FleetOps.Contracts.Maintenance;

public sealed record MaintenanceDue(Guid VehicleId, string Registration, string Service, int IntervalKm, int CurrentKm);
