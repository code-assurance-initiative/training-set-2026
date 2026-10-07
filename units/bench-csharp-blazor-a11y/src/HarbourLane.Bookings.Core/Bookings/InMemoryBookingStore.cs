namespace HarbourLane.Bookings.Bookings;

public sealed class InMemoryBookingStore : IBookingStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Booking> _bookings = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> TryAddAsync(Booking booking, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(booking);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var clash = _bookings.Values.Any(b => b.RoomId == booking.RoomId && b.Overlaps(booking.Start, booking.End));
            if (clash || !_bookings.TryAdd(booking.Reference, booking))
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    public Task<Booking?> FindAsync(string reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_bookings.GetValueOrDefault(reference));
        }
    }

    public Task<IReadOnlyList<Booking>> ListAsync(DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IReadOnlyList<Booking> found = [.. _bookings.Values.Where(b => b.Overlaps(rangeStart, rangeEnd)).OrderBy(b => b.Start)];
            return Task.FromResult(found);
        }
    }

    public Task<IReadOnlyList<Booking>> ListForOrganiserAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IReadOnlyList<Booking> found =
            [
                .. _bookings.Values
                    .Where(b => string.Equals(b.OrganiserEmail, email, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(b => b.Start),
            ];
            return Task.FromResult(found);
        }
    }

    public Task<bool> CancelAsync(string reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_bookings.TryGetValue(reference, out var booking) || booking.Status == BookingStatus.Cancelled)
            {
                return Task.FromResult(false);
            }

            _bookings[reference] = booking with { Status = BookingStatus.Cancelled };
            return Task.FromResult(true);
        }
    }
}
