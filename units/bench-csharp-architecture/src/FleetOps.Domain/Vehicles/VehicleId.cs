namespace FleetOps.Domain.Vehicles;

public readonly record struct VehicleId(Guid Value)
{
    public static VehicleId New() => new(Guid.NewGuid());
}
