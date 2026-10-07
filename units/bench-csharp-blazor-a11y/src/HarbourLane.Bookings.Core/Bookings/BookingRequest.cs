namespace HarbourLane.Bookings.Bookings;

/// <summary>What a visitor fills in on the booking form.</summary>
public sealed class BookingRequest
{
    public Guid RoomId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; } = new(10, 0);

    public int Hours { get; set; } = 1;

    public string OrganiserName { get; set; } = string.Empty;

    public string OrganiserEmail { get; set; } = string.Empty;

    public int Attendees { get; set; } = 1;
}
