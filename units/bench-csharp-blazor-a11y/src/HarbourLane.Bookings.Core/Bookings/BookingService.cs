using HarbourLane.Bookings.Rooms;

namespace HarbourLane.Bookings.Bookings;

public sealed class BookingService(IRoomCatalog rooms, IBookingStore store, TimeProvider clock)
{
    public async Task<BookingOutcome> PlaceAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var room = await rooms.FindAsync(request.RoomId, cancellationToken).ConfigureAwait(false);
        var errors = BookingRequestValidator.Validate(request, room, Today());
        if (room is null || !errors.IsEmpty)
        {
            return new BookingOutcome(null, errors, Conflict: false);
        }

        var start = new DateTimeOffset(request.Date.ToDateTime(request.StartTime), clock.LocalTimeZone.GetUtcOffset(request.Date.ToDateTime(request.StartTime)));
        var booking = new Booking
        {
            Reference = BookingReferences.Next(),
            RoomId = room.Id,
            Start = start,
            End = start.AddHours(request.Hours),
            OrganiserName = request.OrganiserName.Trim(),
            OrganiserEmail = request.OrganiserEmail.Trim(),
            Attendees = request.Attendees,
        };

        var added = await store.TryAddAsync(booking, cancellationToken).ConfigureAwait(false);
        return added ? new BookingOutcome(booking, errors, Conflict: false) : new BookingOutcome(null, errors, Conflict: true);
    }

    public Task<IReadOnlyList<Booking>> ForOrganiserAsync(string email, CancellationToken cancellationToken) =>
        store.ListForOrganiserAsync(email, cancellationToken);

    public async Task<bool> CancelAsync(string reference, string organiserEmail, CancellationToken cancellationToken)
    {
        var booking = await store.FindAsync(reference, cancellationToken).ConfigureAwait(false);
        if (booking is null || !string.Equals(booking.OrganiserEmail, organiserEmail, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return await store.CancelAsync(reference, cancellationToken).ConfigureAwait(false);
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
}
