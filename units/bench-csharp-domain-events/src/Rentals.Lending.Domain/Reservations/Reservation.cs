using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Members;
using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Reservations;

/// <summary>A member's claim on an equipment type for a future window.</summary>
public sealed class Reservation : AggregateRoot<ReservationId>
{
    public Reservation()
        : base(ReservationId.New())
    {
    }

    public MemberId MemberId { get; set; }

    public EquipmentId EquipmentId { get; set; }

    public DateTimeOffset From { get; set; }

    public DateTimeOffset To { get; set; }

    public ReservationStatus Status { get; set; }
}
