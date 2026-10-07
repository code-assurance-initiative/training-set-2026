using ParcelTracking.Core.Carriers;
using ParcelTracking.Core.Parcels;

namespace ParcelTracking.UnitTests.Carriers;

public sealed class CarrierStatusMapTests
{
    [Theory]
    [InlineData("NORDPOST", "DLV", ParcelStatus.Delivered)]
    [InlineData("nordpost", " dlv ", ParcelStatus.Delivered)]
    [InlineData("SWIFTLINE", "with_courier", ParcelStatus.OutForDelivery)]
    [InlineData("SWIFTLINE", "AT_PARCELSHOP", ParcelStatus.HeldAtPickupPoint)]
    public void Known_codes_are_normalised(string carrier, string code, ParcelStatus expected)
    {
        Assert.True(CarrierStatusMap.Default.TryNormalise(carrier, code, out var status));
        Assert.Equal(expected, status);
    }

    [Fact]
    public void Unknown_codes_are_reported_as_unmapped() =>
        Assert.False(CarrierStatusMap.Default.TryNormalise("NORDPOST", "WHAT", out _));

    [Fact]
    public void A_custom_map_is_case_insensitive()
    {
        var map = new CarrierStatusMap([new("Acme:Out", ParcelStatus.OutForDelivery)]);

        Assert.True(map.TryNormalise("ACME", "out", out var status));
        Assert.Equal(ParcelStatus.OutForDelivery, status);
        Assert.Equal(1, map.Count);
    }
}
