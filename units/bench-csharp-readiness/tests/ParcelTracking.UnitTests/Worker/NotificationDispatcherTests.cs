using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Parcels;
using ParcelTracking.UnitTests.TestSupport;
using ParcelTracking.Worker;

namespace ParcelTracking.UnitTests.Worker;

public sealed class NotificationDispatcherTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task Due_notifications_are_delivered_and_marked()
    {
        var store = new FakeParcelStore();
        var due = new PendingNotification(Guid.NewGuid(), Guid.NewGuid(), "merchant-1", "NP100200300", ParcelStatus.InTransit, Now.AddMinutes(-1));
        store.Add(due);
        var notifier = new RecordingNotifier();
        using var services = WorkerHostTestSupport.Services(store, new FakeCarrierClient(), notifier, TimeProvider.System);
        using var dispatcher = new NotificationDispatcher(services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new PollingOptions()), TimeProvider.System);

        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        await WorkerHostTestSupport.WaitUntilAsync(() => due.DeliveredAt is not null);
        await dispatcher.StopAsync(TestContext.Current.CancellationToken);

        Assert.Same(due, Assert.Single(notifier.Sent));
        Assert.True(store.Saves >= 1);
    }

    [Fact]
    public async Task A_failed_delivery_is_scheduled_for_retry()
    {
        var store = new FakeParcelStore();
        var due = new PendingNotification(Guid.NewGuid(), Guid.NewGuid(), "merchant-1", "NP100200300", ParcelStatus.InTransit, Now.AddMinutes(-1));
        store.Add(due);
        using var services = WorkerHostTestSupport.Services(store, new FakeCarrierClient(), new RecordingNotifier { Fail = true }, TimeProvider.System);
        using var dispatcher = new NotificationDispatcher(services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new PollingOptions()), TimeProvider.System);

        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        await WorkerHostTestSupport.WaitUntilAsync(() => due.Attempts == 1);
        await dispatcher.StopAsync(TestContext.Current.CancellationToken);

        Assert.Null(due.DeliveredAt);
        Assert.True(due.NextAttemptAt > Now);
    }
}
