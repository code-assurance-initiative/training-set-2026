using HarbourLane.Bookings.Bookings;
using HarbourLane.Bookings.Rooms;

namespace HarbourLane.Bookings.UnitTests.TestSupport;

internal static class Requests
{
    public static Room Hall => RoomSeed.Rooms[0];

    public static BookingRequest Valid(DateOnly date) => new()
    {
        RoomId = Hall.Id,
        Date = date,
        StartTime = new TimeOnly(10, 0),
        Hours = 2,
        OrganiserName = "Priya Natarajan",
        OrganiserEmail = "priya@harbourlane-choir.org",
        Attendees = 30,
    };
}
