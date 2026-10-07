namespace Depot.Slots.Core.Bookings;

public sealed record BookingRequest(string DockCode, string CarrierReference, DateTimeOffset StartsAt, int DurationMinutes);
