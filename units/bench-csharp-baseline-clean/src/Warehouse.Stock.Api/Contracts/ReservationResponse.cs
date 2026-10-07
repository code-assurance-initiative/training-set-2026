using Warehouse.Stock.Application.Reservations;

namespace Warehouse.Stock.Api.Contracts;

public sealed record ReservationResponse(
    Guid Id,
    string SkuCode,
    string BinCode,
    int Quantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string Status)
{
    public static ReservationResponse From(Reservation reservation) =>
        new(
            reservation.Id,
            reservation.Key.SkuCode,
            reservation.Key.BinCode,
            reservation.Quantity,
            reservation.CreatedAt,
            reservation.ExpiresAt,
            reservation.Status.ToString());
}
