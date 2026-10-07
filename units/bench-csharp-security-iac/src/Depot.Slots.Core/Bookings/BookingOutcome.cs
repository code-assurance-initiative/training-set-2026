namespace Depot.Slots.Core.Bookings;

public enum BookingFailure
{
    None,
    UnknownDock,
    InvalidWindow,
    InThePast,
    Conflict,
}

/// <summary>Either the stored booking or the reason the request was refused.</summary>
public sealed record BookingOutcome(Booking? Booking, BookingFailure Failure, string? Detail)
{
    public bool Succeeded => Booking is not null;

    public static BookingOutcome Booked(Booking booking) => new(booking, BookingFailure.None, null);

    public static BookingOutcome Refused(BookingFailure failure, string detail) => new(null, failure, detail);
}
