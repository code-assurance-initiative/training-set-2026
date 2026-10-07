using System.ComponentModel.DataAnnotations;
using Depot.Slots.Core.Bookings;

namespace Depot.Slots.Api.Endpoints;

public sealed record CreateBookingRequest(
    [property: Required, RegularExpression("^[A-Za-z][0-9]{2}$")] string DockCode,
    [property: Required, StringLength(32, MinimumLength = 3), RegularExpression("^[A-Z0-9-]+$")] string CarrierReference,
    [property: Required] DateTimeOffset? StartsAt,
    [property: Range(15, 480)] int DurationMinutes);

public sealed record BookingResponse(
    Guid Id, string DockCode, string CarrierReference, DateTimeOffset StartsAt, DateTimeOffset EndsAt)
{
    public static BookingResponse From(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);
        return new(booking.Id, booking.DockCode, booking.CarrierReference, booking.StartsAt, booking.EndsAt);
    }
}
