using ParcelTracking.Core.Parcels;

namespace ParcelTracking.UnitTests.Parcels;

public sealed class ParcelTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    private static Parcel NewParcel() => Parcel.Register("NP100200300", "merchant-1", "NORDPOST", "0150", Now);

    private static TrackingEvent Scan(Parcel parcel, ParcelStatus status, int hours, string code = "X") =>
        new(Guid.NewGuid(), parcel.Id, Now.AddHours(hours), code, status, "Oslo");

    [Fact]
    public void A_new_parcel_is_registered_and_counts_as_just_polled()
    {
        var parcel = NewParcel();

        Assert.Equal(ParcelStatus.Registered, parcel.Status);
        Assert.Equal(Now, parcel.LastPolledAt);
        Assert.Empty(parcel.Events);
    }

    [Fact]
    public void A_forward_scan_changes_the_status()
    {
        var parcel = NewParcel();

        Assert.True(parcel.Record(Scan(parcel, ParcelStatus.InTransit, 1)));
        Assert.Equal(ParcelStatus.InTransit, parcel.Status);
    }

    [Fact]
    public void A_late_backward_scan_is_kept_as_history_without_moving_the_status()
    {
        var parcel = NewParcel();
        parcel.Record(Scan(parcel, ParcelStatus.OutForDelivery, 2, "OFD"));

        Assert.False(parcel.Record(Scan(parcel, ParcelStatus.InTransit, 1, "HUB")));
        Assert.Equal(ParcelStatus.OutForDelivery, parcel.Status);
        Assert.Equal(2, parcel.Events.Count);
    }

    [Fact]
    public void A_resent_scan_is_ignored()
    {
        var parcel = NewParcel();
        var scan = Scan(parcel, ParcelStatus.InTransit, 1, "HUB");
        parcel.Record(scan);

        Assert.False(parcel.Record(new TrackingEvent(Guid.NewGuid(), parcel.Id, scan.OccurredAt, "HUB", ParcelStatus.InTransit, null)));
        Assert.Single(parcel.Events);
    }

    [Fact]
    public void Delivery_stamps_the_delivery_time()
    {
        var parcel = NewParcel();
        parcel.Record(Scan(parcel, ParcelStatus.Delivered, 30, "DLV"));

        Assert.Equal(Now.AddHours(30), parcel.DeliveredAt);
    }

    [Fact]
    public void A_delivered_parcel_cannot_be_redirected()
    {
        var parcel = NewParcel();
        parcel.Record(Scan(parcel, ParcelStatus.Delivered, 30, "DLV"));

        Assert.Throws<InvalidOperationException>(() => parcel.RedirectToPickupPoint("PUP-1", null, null));
    }

    [Fact]
    public void Redirecting_records_the_pickup_point()
    {
        var parcel = NewParcel();

        parcel.RedirectToPickupPoint("PUP-77", new DateOnly(2026, 9, 10), "Leave with the kiosk");

        Assert.Equal("PUP-77", parcel.PickupPointId);
        Assert.Equal(new DateOnly(2026, 9, 10), parcel.HoldUntil);
    }

    [Theory]
    [InlineData("", "m", "c", "p")]
    [InlineData("t", " ", "c", "p")]
    [InlineData("t", "m", "", "p")]
    [InlineData("t", "m", "c", " ")]
    public void Registration_requires_every_field(string tracking, string merchant, string carrier, string postal) =>
        Assert.ThrowsAny<ArgumentException>(() => Parcel.Register(tracking, merchant, carrier, postal, Now));
}
