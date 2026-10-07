using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ParcelTracking.Core.Carriers;
using ParcelTracking.Core.Parcels;
using ParcelTracking.UnitTests.TestSupport;
using ParcelTracking.Worker;

namespace ParcelTracking.UnitTests.Worker;

public sealed class TrackingPollerTests
{
    [Fact]
    public async Task A_polling_round_refreshes_parcels_that_are_due()
    {
        var registeredAt = DateTimeOffset.UtcNow.AddHours(-1);
        var store = new FakeParcelStore();
        var parcel = Parcel.Register("NP100200300", "merchant-1", "NORDPOST", "0150", registeredAt);
        store.Add(parcel);
        var carrier = new FakeCarrierClient();
        carrier.Scans["NP100200300"] = [new CarrierScan("HUB", registeredAt.AddMinutes(10), "Oslo")];
        using var services = WorkerHostTestSupport.Services(store, carrier, new RecordingNotifier(), TimeProvider.System);
        using var poller = new TrackingPoller(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new PollingOptions { PollAge = TimeSpan.FromMinutes(15), Interval = TimeSpan.FromMinutes(10) }),
            TimeProvider.System,
            NullLogger<TrackingPoller>.Instance);

        await poller.StartAsync(TestContext.Current.CancellationToken);
        await WorkerHostTestSupport.WaitUntilAsync(() => parcel.Status == ParcelStatus.InTransit);
        using var stopWithin = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await poller.StopAsync(stopWithin.Token);

        Assert.Single(store.Notifications);
        Assert.True(parcel.LastPolledAt > registeredAt);
    }
}
