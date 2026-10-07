using Microsoft.Extensions.Options;
using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Tracking;

namespace ParcelTracking.Worker;

/// <summary>
/// Delivers the outbox: every pending status-change notification whose next attempt is due is posted to the
/// merchant-hooks relay; failures are retried with exponential back-off.
/// </summary>
public sealed class NotificationDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<PollingOptions> options,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.NotificationInterval, timeProvider);
        try
        {
            do
            {
                await DispatchDueAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task DispatchDueAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IParcelStore>();
        var notifier = scope.ServiceProvider.GetRequiredService<IMerchantNotifier>();

        var due = await store.ListDueNotificationsAsync(timeProvider.GetUtcNow(), options.Value.BatchSize, cancellationToken);
        foreach (var notification in due)
        {
            try
            {
                await notifier.NotifyAsync(notification, cancellationToken);
                notification.MarkDelivered(timeProvider.GetUtcNow());
                Console.WriteLine($"Delivered {notification.Status} for {notification.TrackingNumber} to merchant {notification.MerchantId}");
            }
            catch (HttpRequestException ex)
            {
                notification.MarkFailed(timeProvider.GetUtcNow());
                Console.Error.WriteLine($"Webhook for {notification.TrackingNumber} failed (attempt {notification.Attempts}): {ex.Message}");
            }
        }

        await store.SaveChangesAsync(cancellationToken);
    }
}
