namespace Quellbrook.Notifier.Persistence;

/// <summary>
/// Who to notify about an order, as announced by the order service: the consignee's name and optional contact
/// details. Kept only until 30 days after the delivery (docs/privacy.md).
/// </summary>
public sealed class Recipient
{
    public Guid OrderId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>When the last notification for the order was sent (delivery); the retention clock starts here.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>A message already processed, by its AMQP message id.</summary>
public sealed class ProcessedMessage
{
    public Guid MessageId { get; set; }

    public string Type { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; }
}

/// <summary>One notification sent: what, when, through which channel, to a masked recipient.</summary>
public sealed class NotificationLogEntry
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Channel { get; set; } = string.Empty;

    public string MaskedRecipient { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; }
}
