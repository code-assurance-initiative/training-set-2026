using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Parcels;
using ParcelTracking.UnitTests.TestSupport;
using ParcelTracking.Worker;

namespace ParcelTracking.UnitTests.Worker;

public sealed class RetentionSweeperTests
{
    [Fact]
    public async Task Only_data_past_its_retention_period_is_deleted()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero));
        var now = clock.GetUtcNow();
        var store = new FakeParcelStore();
        var oldDelivered = Parcel.Register("NP000000001", "m", "NORDPOST", "0150", now.AddDays(-400));
        oldDelivered.Record(new TrackingEvent(Guid.NewGuid(), oldDelivered.Id, now.AddDays(-399), "DLV", ParcelStatus.Delivered, null));
        var oldInTransit = Parcel.Register("NP000000002", "m", "NORDPOST", "0150", now.AddDays(-400));
        store.Add(oldDelivered);
        store.Add(oldInTransit);
        var sent = new PendingNotification(Guid.NewGuid(), oldDelivered.Id, "m", "NP000000001", ParcelStatus.Delivered, now.AddDays(-40));
        sent.MarkDelivered(now.AddDays(-40));
        var recent = new PendingNotification(Guid.NewGuid(), oldDelivered.Id, "m", "NP000000001", ParcelStatus.Delivered, now.AddDays(-2));
        recent.MarkDelivered(now.AddDays(-2));
        store.Add(sent);
        store.Add(recent);
        using var services = WorkerHostTestSupport.Services(store, new FakeCarrierClient(), new RecordingNotifier(), clock);
        using var sweeper = new RetentionSweeper(services.GetRequiredService<IServiceScopeFactory>(), Options.Create(new RetentionOptions()), clock, NullLogger<RetentionSweeper>.Instance);

        var deleted = await sweeper.SweepAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, deleted);
        Assert.Same(oldInTransit, Assert.Single(store.Parcels));
        Assert.Same(recent, Assert.Single(store.Notifications));
    }
}
