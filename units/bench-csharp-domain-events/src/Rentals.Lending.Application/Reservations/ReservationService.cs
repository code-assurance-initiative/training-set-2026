using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Members;
using Rentals.Lending.Domain.Reservations;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Reservations;

/// <summary>Places, confirms and cancels reservations of an equipment type for a future window.</summary>
public sealed class ReservationService(
    IReservationRepository reservations,
    IMemberRepository members,
    IEquipmentRepository equipment,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    private static readonly TimeSpan MaximumWindow = TimeSpan.FromDays(14);

    public async Task<ReservationId> ReserveAsync(
        MemberId memberId, EquipmentId equipmentId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var member = await members.GetAsync(memberId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Member", memberId);
        if (member.Status != MemberStatus.Active || member.MembershipExpiresAt <= now)
        {
            throw new DomainRuleViolationException("Only a member in good standing can reserve equipment.");
        }

        if (from < now || to <= from || to - from > MaximumWindow)
        {
            throw new DomainRuleViolationException("A reservation is a future window of at most 14 days.");
        }

        var item = await equipment.GetAsync(equipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Equipment", equipmentId);
        var active = await reservations.ListActiveForEquipmentAsync(item.Id, cancellationToken).ConfigureAwait(false);
        var overlapping = active.Count(r => r.From < to && from < r.To);
        if (overlapping >= item.Units.Count(u => u.IsLendable))
        {
            throw new DomainRuleViolationException($"No unit of {item.Name} is free in that window.");
        }

        var reservation = new Reservation
        {
            MemberId = memberId,
            EquipmentId = equipmentId,
            From = from,
            To = to,
            Status = ReservationStatus.Pending,
        };
        await reservations.AddAsync(reservation, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return reservation.Id;
    }

    public async Task ConfirmAsync(ReservationId reservationId, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetAsync(reservationId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Reservation", reservationId);
        if (reservation.Status == ReservationStatus.Pending)
        {
            reservation.Status = ReservationStatus.Confirmed;
        }

        await reservations.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelAsync(ReservationId reservationId, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetAsync(reservationId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Reservation", reservationId);
        reservation.Status = ReservationStatus.Cancelled;
        await reservations.UpdateAsync(reservation, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
