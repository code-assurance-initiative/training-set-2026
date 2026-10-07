namespace HarbourLane.Bookings.Bookings;

public sealed record BookingOutcome(Booking? Booking, BookingErrors Errors, bool Conflict)
{
    public bool Succeeded => Booking is not null;
}
