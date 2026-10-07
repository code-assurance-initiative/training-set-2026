using Rentals.Billing.Domain.EventSourcing;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Billing.Infrastructure;

/// <summary>
/// An event store held in memory, for development and tests. Appends, the global log and the consumers' inbox share
/// one lock, so an append and its inbox receipt are recorded atomically — the same guarantee a database-backed store
/// gives by writing both in one transaction. Events are kept in their serialised form, as a database-backed store
/// keeps them, so every read goes through the same deserialisation a durable store needs.
/// </summary>
public sealed class InMemoryEventStore : IEventStore, IInboxStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, List<SerializedEvent>> _streams = new(StringComparer.Ordinal);
    private readonly List<LogEntry> _log = [];
    private readonly HashSet<(string Consumer, Guid MessageId)> _inbox = [];

    public Task<bool> ExistsAsync(string streamId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_streams.ContainsKey(streamId));
        }
    }

    public Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(string streamId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IReadOnlyList<IDomainEvent> events = _streams.TryGetValue(streamId, out var stream)
                ? [.. stream.Select(AccountEventSerializer.Deserialize)]
                : [];
            return Task.FromResult(events);
        }
    }

    public Task AppendAsync(
        string streamId, long expectedVersion, IReadOnlyList<IDomainEvent> events, InboxReceipt? receipt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(events);
        var serialized = events.Select(AccountEventSerializer.Serialize).ToList();
        lock (_gate)
        {
            var stream = _streams.TryGetValue(streamId, out var existing) ? existing : [];
            if (stream.Count != expectedVersion)
            {
                throw new ConcurrencyException($"Stream {streamId} is at version {stream.Count}, not {expectedVersion}.");
            }

            if (receipt is not null && !_inbox.Add((receipt.Consumer, receipt.MessageId)))
            {
                throw new ConcurrencyException($"{receipt.Consumer} already processed message {receipt.MessageId}.");
            }

            foreach (var storedForm in serialized)
            {
                stream.Add(storedForm);
                _log.Add(new LogEntry(streamId, stream.Count, _log.Count + 1, storedForm));
            }

            _streams[streamId] = stream;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoredEvent>> ReadAllAsync(long fromPosition, int maxCount, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IReadOnlyList<StoredEvent> page =
            [
                .. _log.Where(e => e.Position > fromPosition).Take(maxCount)
                    .Select(e => new StoredEvent(e.StreamId, e.Version, e.Position, AccountEventSerializer.Deserialize(e.Event))),
            ];
            return Task.FromResult(page);
        }
    }

    public Task<bool> HasProcessedAsync(string consumer, Guid messageId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_inbox.Contains((consumer, messageId)));
        }
    }

    private sealed record LogEntry(string StreamId, long Version, long Position, SerializedEvent Event);
}
