using Rentals.SharedKernel;

namespace Rentals.Billing.Domain.EventSourcing;

/// <summary>An append-only log of event streams, with optimistic concurrency per stream.</summary>
public interface IEventStore
{
    Task<bool> ExistsAsync(string streamId, CancellationToken cancellationToken);

    Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(string streamId, CancellationToken cancellationToken);

    /// <summary>
    /// Appends <paramref name="events"/> if the stream is still at <paramref name="expectedVersion"/>. When
    /// <paramref name="receipt"/> is given, the consumer's processed message is recorded atomically with the append.
    /// </summary>
    Task AppendAsync(
        string streamId, long expectedVersion, IReadOnlyList<IDomainEvent> events, InboxReceipt? receipt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StoredEvent>> ReadAllAsync(long fromPosition, int maxCount, CancellationToken cancellationToken);
}
