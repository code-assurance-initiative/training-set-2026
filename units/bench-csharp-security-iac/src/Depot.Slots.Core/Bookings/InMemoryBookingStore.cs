namespace Depot.Slots.Core.Bookings;

/// <summary>
/// A process-local store for development and tests. It enforces the same no-overlap rule as the database
/// constraint, under one lock.
/// </summary>
public sealed class InMemoryBookingStore : IBookingStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, Booking> _bookings = [];

    public Task<IReadOnlyList<Booking>> ListForDockAsync(
        string dockCode, DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IReadOnlyList<Booking> result = _bookings.Values
                .Where(b => b.DockCode == dockCode && b.Overlaps(windowStart, windowEnd))
                .OrderBy(b => b.StartsAt)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<bool> TryAddAsync(Booking booking, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(booking);
        lock (_gate)
        {
            var taken = _bookings.Values.Any(b => b.DockCode == booking.DockCode && b.Overlaps(booking.StartsAt, booking.EndsAt));
            if (taken)
            {
                return Task.FromResult(false);
            }

            _bookings.Add(booking.Id, booking);
            return Task.FromResult(true);
        }
    }

    public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_bookings.Remove(id));
        }
    }

    public Task<IReadOnlyList<Booking>> ListDueForReminderAsync(DateTimeOffset until, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IReadOnlyList<Booking> result = _bookings.Values
                .Where(b => b.ReminderSentAt is null && b.StartsAt <= until)
                .OrderBy(b => b.StartsAt)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task MarkRemindedAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_bookings.TryGetValue(id, out var booking))
            {
                _bookings[id] = booking with { ReminderSentAt = sentAt };
            }

            return Task.CompletedTask;
        }
    }
}
