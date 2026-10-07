namespace Rentals.Billing.Domain.EventSourcing;

/// <summary>Proof that <paramref name="Consumer"/> has applied the message <paramref name="MessageId"/>.</summary>
public sealed record InboxReceipt(string Consumer, Guid MessageId);
