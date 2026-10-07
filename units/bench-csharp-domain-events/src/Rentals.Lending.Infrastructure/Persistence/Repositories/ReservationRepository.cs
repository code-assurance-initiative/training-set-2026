using Microsoft.EntityFrameworkCore;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Reservations;

namespace Rentals.Lending.Infrastructure.Persistence.Repositories;

internal sealed class ReservationRepository(LendingDbContext db) : IReservationRepository
{
    public Task<Reservation?> GetAsync(ReservationId id, CancellationToken cancellationToken) =>
        db.Reservations.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reservation>> ListActiveForEquipmentAsync(EquipmentId equipmentId, CancellationToken cancellationToken) =>
        await db.Reservations
            .Where(r => r.EquipmentId == equipmentId && r.Status != ReservationStatus.Cancelled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Reservation entity, CancellationToken cancellationToken) =>
        await db.Reservations.AddAsync(entity, cancellationToken).ConfigureAwait(false);

    public Task UpdateAsync(Reservation entity, CancellationToken cancellationToken)
    {
        db.Reservations.Update(entity);
        return Task.CompletedTask;
    }
}
