using Microsoft.EntityFrameworkCore;
using Rentals.Lending.Domain.Catalogue;

namespace Rentals.Lending.Infrastructure.Persistence.Repositories;

internal sealed class EquipmentRepository(LendingDbContext db) : IEquipmentRepository
{
    public Task<Equipment?> GetAsync(EquipmentId id, CancellationToken cancellationToken) =>
        db.Equipment.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Equipment?> FindByUnitAsync(EquipmentUnitId unitId, CancellationToken cancellationToken) =>
        db.Equipment.SingleOrDefaultAsync(e => e.Units.Any(u => u.Id == unitId), cancellationToken);

    public async Task AddAsync(Equipment entity, CancellationToken cancellationToken) =>
        await db.Equipment.AddAsync(entity, cancellationToken).ConfigureAwait(false);

    public Task UpdateAsync(Equipment entity, CancellationToken cancellationToken)
    {
        db.Equipment.Update(entity);
        return Task.CompletedTask;
    }
}
