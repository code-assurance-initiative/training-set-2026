using Microsoft.EntityFrameworkCore;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Maintenance;

namespace Rentals.Lending.Infrastructure.Persistence.Repositories;

internal sealed class MaintenanceRecordRepository(LendingDbContext db) : IMaintenanceRecordRepository
{
    public Task<MaintenanceRecord?> GetAsync(MaintenanceRecordId id, CancellationToken cancellationToken) =>
        db.MaintenanceRecords.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceRecord>> ListForUnitAsync(EquipmentUnitId unitId, CancellationToken cancellationToken) =>
        await db.MaintenanceRecords.Where(r => r.UnitId == unitId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(MaintenanceRecord entity, CancellationToken cancellationToken) =>
        await db.MaintenanceRecords.AddAsync(entity, cancellationToken).ConfigureAwait(false);

    public Task UpdateAsync(MaintenanceRecord entity, CancellationToken cancellationToken)
    {
        db.MaintenanceRecords.Update(entity);
        return Task.CompletedTask;
    }
}
