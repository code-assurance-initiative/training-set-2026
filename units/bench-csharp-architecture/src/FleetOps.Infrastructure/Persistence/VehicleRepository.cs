using FleetOps.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Infrastructure.Persistence;

public sealed class VehicleRepository(FleetOpsDbContext db) : IVehicleRepository
{
    public Task<Vehicle?> FindAsync(VehicleId id, CancellationToken cancellationToken) =>
        db.Vehicles.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Vin vin, CancellationToken cancellationToken) =>
        db.Vehicles.AnyAsync(v => v.Vin == vin, cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> ListActiveAsync(CancellationToken cancellationToken) =>
        await db.Vehicles.Where(v => v.Status != VehicleStatus.Retired).OrderBy(v => v.Registration)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(Vehicle vehicle) => db.Vehicles.Add(vehicle);
}
