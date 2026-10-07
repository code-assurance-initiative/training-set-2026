namespace Depot.Slots.Core.Bookings;

/// <summary>A carrier's reservation of one loading dock for a time window.</summary>
public sealed record Booking(
    Guid Id,
    string DockCode,
    string CarrierReference,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? ReminderSentAt)
{
    public bool Overlaps(DateTimeOffset startsAt, DateTimeOffset endsAt) => StartsAt < endsAt && startsAt < EndsAt;
}
