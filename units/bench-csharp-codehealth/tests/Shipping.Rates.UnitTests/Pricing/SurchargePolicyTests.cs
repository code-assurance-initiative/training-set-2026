using Shipping.Rates.Core.Domain;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Pricing;

public sealed class SurchargePolicyTests
{
    [Fact]
    public void Fuel_is_capped_at_the_configured_share()
    {
        var policy = TestData.Policy();

        Assert.Equal(1.20m, policy.FuelSurcharge("ALDER", 10m));
    }

    [Fact]
    public void Markup_applies_to_every_fee()
    {
        var policy = TestData.Policy(markup: 1.1m);

        Assert.Equal(18.50m * 1.1m, policy.OversizeFee("ALDER"));
        Assert.Equal(7.50m * 1.1m, policy.RemoteAreaFee("CORVID"));
        Assert.Equal(4.50m * 1.1m, policy.CustomsFee("CORVID"));
        Assert.Equal(12.00m * 1.1m, policy.HeavyFee("ALDER"));
    }

    [Fact]
    public void Dangerous_goods_are_charged_per_started_kilogram()
    {
        var policy = TestData.Policy();

        Assert.Equal(24.00m * 2, policy.DangerousGoodsFee("ALDER", new Parcel(1_001, 10, 10, 10, IsDangerousGoods: true)));
    }

    [Fact]
    public void Unknown_surcharges_are_reported()
    {
        var policy = TestData.Policy();

        Assert.True(policy.HasSurcharge("CORVID", "fuel"));
        Assert.False(policy.HasSurcharge("CORVID", "saturday"));
        Assert.Throws<InvalidOperationException>(() => policy.OversizeFee("NOPE"));
    }
}
