namespace FleetOps.Contracts.Vehicles;

public sealed record VehicleSummary(
    Guid Id,
    string Vin,
    string Registration,
    string Model,
    int OdometerKm,
    int LastServiceKm,
    string Status);
