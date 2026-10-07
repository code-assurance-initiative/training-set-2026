namespace FleetOps.Domain.Vehicles;

public interface IVehicleRepository
{
    Task<Vehicle?> FindAsync(VehicleId id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Vin vin, CancellationToken cancellationToken);

    Task<IReadOnlyList<Vehicle>> ListActiveAsync(CancellationToken cancellationToken);

    void Add(Vehicle vehicle);
}
