namespace HarbourLane.Bookings.Bookings;

public interface IBookingStore
{
    /// <summary>Adds the booking unless a confirmed booking of the same room overlaps it.</summary>
    Task<bool> TryAddAsync(Booking booking, CancellationToken cancellationToken);

    Task<Booking?> FindAsync(string reference, CancellationToken cancellationToken);

    Task<IReadOnlyList<Booking>> ListAsync(DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken cancellationToken);

    Task<IReadOnlyList<Booking>> ListForOrganiserAsync(string email, CancellationToken cancellationToken);

    Task<bool> CancelAsync(string reference, CancellationToken cancellationToken);
}
