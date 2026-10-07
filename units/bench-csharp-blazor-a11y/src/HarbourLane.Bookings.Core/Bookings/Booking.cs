namespace HarbourLane.Bookings.Bookings;

public sealed record Booking
{
    public required string Reference { get; init; }

    public required Guid RoomId { get; init; }

    public required DateTimeOffset Start { get; init; }

    public required DateTimeOffset End { get; init; }

    public required string OrganiserName { get; init; }

    public required string OrganiserEmail { get; init; }

    public required int Attendees { get; init; }

    public BookingStatus Status { get; init; } = BookingStatus.Confirmed;

    public bool Overlaps(DateTimeOffset start, DateTimeOffset end) =>
        Status == BookingStatus.Confirmed && Start < end && start < End;
}
