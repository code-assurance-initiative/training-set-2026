namespace Depot.Slots.Core.Bookings;

public interface IBookingStore
{
    Task<IReadOnlyList<Booking>> ListForDockAsync(
        string dockCode, DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken cancellationToken);

    /// <summary>Adds the booking unless it overlaps an existing booking of the same dock.</summary>
    /// <returns><see langword="false"/> when the window is already taken.</returns>
    Task<bool> TryAddAsync(Booking booking, CancellationToken cancellationToken);

    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bookings that start at or before <paramref name="until"/> and have not been announced yet.</summary>
    Task<IReadOnlyList<Booking>> ListDueForReminderAsync(DateTimeOffset until, CancellationToken cancellationToken);

    Task MarkRemindedAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken);
}
