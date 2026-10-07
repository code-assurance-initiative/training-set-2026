using Quellbrook.Notifier.Messaging;
using Quellbrook.Notifier.Persistence;

namespace Quellbrook.Notifier.Notifications;

/// <summary>Keeps the consignee's contact details for the order and confirms the booking by e-mail.</summary>
public sealed class OrderPlacedHandler(NotifierDbContext db, NotificationService notifications, TimeProvider time)
{
    public async Task HandleAsync(OrderPlacedMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var recipient = await db.Recipients.FindAsync([message.OrderId], cancellationToken).ConfigureAwait(false);
        if (recipient is null)
        {
            recipient = new Recipient
            {
                OrderId = message.OrderId,
                Name = message.Consignee.Name,
                Email = message.Consignee.Contact.Email,
                Phone = message.Consignee.Contact.Phone,
                ReceivedAt = time.GetUtcNow(),
            };
            db.Recipients.Add(recipient);
        }

        await notifications.NotifyAsync(recipient, NotificationKind.OrderConfirmed, cancellationToken).ConfigureAwait(false);
    }
}
