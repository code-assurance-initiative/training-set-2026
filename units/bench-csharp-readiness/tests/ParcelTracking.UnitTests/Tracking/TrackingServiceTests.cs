using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ParcelTracking.Core.Carriers;
using ParcelTracking.Core.Parcels;
using ParcelTracking.Core.Tracking;
using ParcelTracking.UnitTests.TestSupport;

namespace ParcelTracking.UnitTests.Tracking;

public sealed class TrackingServiceTests
{
    private readonly FakeParcelStore _store = new();
    private readonly FakeCarrierClient _carrier = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly TrackingService _service;

    public TrackingServiceTests()
    {
        _service = new TrackingService(_store, _carrier, CarrierStatusMap.Default, _clock, NullLogger<TrackingService>.Instance);
    }

    [Fact]
    public async Task Registering_twice_returns_null_the_second_time()
    {
        var first = await _service.RegisterAsync("NP100200300", "merchant-1", "NORDPOST", "0150", TestContext.Current.CancellationToken);
        var second = await _service.RegisterAsync("NP100200300", "merchant-2", "NORDPOST", "0150", TestContext.Current.CancellationToken);

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Single(_store.Parcels);
    }

    [Fact]
    public async Task Refreshing_records_scans_and_queues_one_notification_per_status_change()
    {
        var parcel = await _service.RegisterAsync("NP100200300", "merchant-1", "NORDPOST", "0150", TestContext.Current.CancellationToken);
        Assert.NotNull(parcel);
        var now = _clock.GetUtcNow();
        _carrier.Scans["NP100200300"] =
        [
            new CarrierScan("OFD", now.AddHours(5), "Oslo"),
            new CarrierScan("ACC", now.AddHours(1), "Bergen"),
            new CarrierScan("HUB", now.AddHours(3), "Oslo"),
        ];
        _clock.Advance(TimeSpan.FromHours(6));

        var changes = await _service.RefreshAsync(parcel, TestContext.Current.CancellationToken);

        Assert.Equal(2, changes);
        Assert.Equal(ParcelStatus.OutForDelivery, parcel.Status);
        Assert.Equal(3, parcel.Events.Count);
        Assert.Equal([ParcelStatus.InTransit, ParcelStatus.OutForDelivery], _store.Notifications.Select(n => n.Status));
        Assert.Equal(_clock.GetUtcNow(), parcel.LastPolledAt);
    }

    [Fact]
    public async Task Unmapped_carrier_codes_are_skipped()
    {
        var parcel = await _service.RegisterAsync("NP100200300", "merchant-1", "NORDPOST", "0150", TestContext.Current.CancellationToken);
        Assert.NotNull(parcel);
        _carrier.Scans["NP100200300"] = [new CarrierScan("ZZZ", _clock.GetUtcNow().AddHours(1), null)];

        var changes = await _service.RefreshAsync(parcel, TestContext.Current.CancellationToken);

        Assert.Equal(0, changes);
        Assert.Empty(parcel.Events);
        Assert.Empty(_store.Notifications);
    }

    [Fact]
    public async Task Redirect_outcomes()
    {
        Assert.Equal(RedirectOutcome.NotFound, await _service.RedirectAsync("NOPE0000", "PUP-1", null, null, TestContext.Current.CancellationToken));

        var parcel = await _service.RegisterAsync("NP100200300", "merchant-1", "NORDPOST", "0150", TestContext.Current.CancellationToken);
        Assert.NotNull(parcel);
        Assert.Equal(RedirectOutcome.Redirected, await _service.RedirectAsync("NP100200300", "PUP-1", null, null, TestContext.Current.CancellationToken));

        parcel.Record(new TrackingEvent(Guid.NewGuid(), parcel.Id, _clock.GetUtcNow().AddDays(1), "DLV", ParcelStatus.Delivered, null));
        Assert.Equal(RedirectOutcome.AlreadyCompleted, await _service.RedirectAsync("NP100200300", "PUP-2", null, null, TestContext.Current.CancellationToken));
    }
}
