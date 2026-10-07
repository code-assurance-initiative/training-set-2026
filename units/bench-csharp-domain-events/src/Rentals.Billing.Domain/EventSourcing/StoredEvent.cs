using Rentals.SharedKernel;

namespace Rentals.Billing.Domain.EventSourcing;

/// <summary>An event as the store keeps it: its stream, its version in the stream and its position in the whole log.</summary>
public sealed record StoredEvent(string StreamId, long Version, long Position, IDomainEvent Event);
