using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Depot.Slots.Core.Bookings;

public sealed partial class BookingService(
    IBookingStore store,
    IOptions<DockOptions> options,
    TimeProvider clock,
    ILogger<BookingService> logger)
{
    private readonly DockOptions _docks = options.Value;

    public async Task<BookingOutcome> BookAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dock = request.DockCode.ToUpperInvariant();
        if (!_docks.Codes.Contains(dock, StringComparer.Ordinal))
        {
            return BookingOutcome.Refused(BookingFailure.UnknownDock, $"Dock '{request.DockCode}' does not exist.");
        }

        if (!IsAlignedWindow(request))
        {
            return BookingOutcome.Refused(
                BookingFailure.InvalidWindow,
                $"Slots start on a {_docks.SlotMinutes}-minute boundary and last at most {_docks.MaxDurationMinutes} minutes.");
        }

        if (request.StartsAt <= clock.GetUtcNow())
        {
            return BookingOutcome.Refused(BookingFailure.InThePast, "The slot has already started.");
        }

        var booking = new Booking(
            Guid.NewGuid(),
            dock,
            request.CarrierReference,
            request.StartsAt.ToUniversalTime(),
            request.StartsAt.ToUniversalTime().AddMinutes(request.DurationMinutes),
            ReminderSentAt: null);

        if (!await store.TryAddAsync(booking, cancellationToken).ConfigureAwait(false))
        {
            return BookingOutcome.Refused(BookingFailure.Conflict, "The dock is already booked for part of that window.");
        }

        LogBooked(booking.Id, booking.DockCode, booking.StartsAt);
        return BookingOutcome.Booked(booking);
    }

    public Task<IReadOnlyList<Booking>> ListDayAsync(string dockCode, DateOnly day, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dockCode);
        var from = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return store.ListForDockAsync(dockCode.ToUpperInvariant(), from, from.AddDays(1), cancellationToken);
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var removed = await store.RemoveAsync(id, cancellationToken).ConfigureAwait(false);
        if (removed)
        {
            LogCancelled(id);
        }

        return removed;
    }

    private bool IsAlignedWindow(BookingRequest request)
    {
        var slot = _docks.SlotMinutes;
        return request.DurationMinutes > 0
            && request.DurationMinutes <= _docks.MaxDurationMinutes
            && request.DurationMinutes % slot == 0
            && request.StartsAt.Second == 0
            && request.StartsAt.Millisecond == 0
            && request.StartsAt.Minute % slot == 0;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Booked {BookingId} on dock {DockCode} at {StartsAt}")]
    private partial void LogBooked(Guid bookingId, string dockCode, DateTimeOffset startsAt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cancelled booking {BookingId}")]
    private partial void LogCancelled(Guid bookingId);
}
