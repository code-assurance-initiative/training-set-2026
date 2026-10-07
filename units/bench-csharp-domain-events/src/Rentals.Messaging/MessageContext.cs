namespace Rentals.Messaging;

/// <summary>Transport metadata delivered with every message. <see cref="MessageId"/> is stable across redeliveries.</summary>
public sealed record MessageContext(Guid MessageId, string MessageType, DateTimeOffset SentAt);
