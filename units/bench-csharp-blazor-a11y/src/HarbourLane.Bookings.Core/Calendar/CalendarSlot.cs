using HarbourLane.Bookings.Bookings;

namespace HarbourLane.Bookings.Calendar;

/// <summary>One hour of one day in the week view, and the booking that holds it, if any.</summary>
public sealed record CalendarSlot(DateOnly Day, int Hour, Booking? Booking)
{
    public bool IsFree => Booking is null;
}
