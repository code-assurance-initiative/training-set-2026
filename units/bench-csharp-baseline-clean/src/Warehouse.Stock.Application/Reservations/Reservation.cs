using Warehouse.Stock.Application.Inventory;

namespace Warehouse.Stock.Application.Reservations;

/// <summary>A hold on a quantity of one SKU in one bin, which lapses at <see cref="ExpiresAt"/> unless settled.</summary>
public sealed record Reservation(
    Guid Id,
    StockKey Key,
    int Quantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    ReservationStatus Status)
{
    public bool IsActive => Status == ReservationStatus.Active;

    public bool IsDueBy(DateTimeOffset instant) => IsActive && ExpiresAt <= instant;

    public Reservation WithStatus(ReservationStatus status) => this with { Status = status };
}
