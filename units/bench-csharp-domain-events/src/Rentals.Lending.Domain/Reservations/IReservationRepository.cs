using Rentals.Lending.Domain.Catalogue;
using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Reservations;

public interface IReservationRepository : IRepository<Reservation, ReservationId>
{
    Task<IReadOnlyList<Reservation>> ListActiveForEquipmentAsync(EquipmentId equipmentId, CancellationToken cancellationToken);
}
