using Microsoft.Extensions.Logging;
using Quellbrook.Notifier.Messaging;
using Quellbrook.Notifier.Persistence;

namespace Quellbrook.Notifier.Notifications;

/// <summary>Tells the consignee that the parcels are on their way today, and that they were delivered.</summary>
public sealed partial class DeliveryHandlers(
    NotifierDbContext db,
    NotificationService notifications,
    ILogger<DeliveryHandlers> logger)
{
    public async Task HandleAsync(ConsignmentOutForDeliveryMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (await FindAsync(message.OrderId, cancellationToken).ConfigureAwait(false) is { } recipient)
        {
            await notifications.NotifyAsync(recipient, NotificationKind.OutForDelivery, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task HandleAsync(ConsignmentDeliveredMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (await FindAsync(message.OrderId, cancellationToken).ConfigureAwait(false) is { } recipient)
        {
            await notifications.NotifyAsync(recipient, NotificationKind.Delivered, cancellationToken).ConfigureAwait(false);
            recipient.CompletedAt = message.DeliveredAt;
        }
    }

    private async Task<Recipient?> FindAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var recipient = await db.Recipients.FindAsync([orderId], cancellationToken).ConfigureAwait(false);
        if (recipient is null)
        {
            LogUnknownOrder(orderId);
        }

        return recipient;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No recipient for order {OrderId}; the order was never announced or its data has been deleted")]
    private partial void LogUnknownOrder(Guid orderId);
}
