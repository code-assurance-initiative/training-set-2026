namespace Rentals.Messaging;

/// <summary>
/// Remembers which messages a consumer has processed, so a redelivery can be recognised and skipped. The receipt is
/// written by the store that holds the consumer's state, in the same transaction as the state change.
/// </summary>
public interface IInboxStore
{
    Task<bool> HasProcessedAsync(string consumer, Guid messageId, CancellationToken cancellationToken);
}
