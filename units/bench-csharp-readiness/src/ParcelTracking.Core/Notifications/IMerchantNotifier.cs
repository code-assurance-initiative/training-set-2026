namespace ParcelTracking.Core.Notifications;

public interface IMerchantNotifier
{
    Task NotifyAsync(PendingNotification notification, CancellationToken cancellationToken);
}
