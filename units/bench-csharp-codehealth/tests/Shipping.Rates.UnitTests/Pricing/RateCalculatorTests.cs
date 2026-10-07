using Shipping.Rates.Core.Domain;
using Shipping.Rates.Core.Pricing;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Pricing;

public sealed class RateCalculatorTests
{
    private readonly RateCalculator _calculator = TestData.Calculator();

    [Fact]
    public void Alder_standard_to_a_neighbour_is_tariff_plus_fuel()
    {
        var price = _calculator.QuoteParcel("ALDER", ServiceLevel.Standard, TestData.Small, TestData.Paris);

        Assert.Equal(new Money(7.00m, "EUR"), price);
    }

    [Fact]
    public void Corvid_charges_its_own_fuel_rate()
    {
        var price = _calculator.QuoteParcel("CORVID", ServiceLevel.Standard, TestData.Small, TestData.Paris);

        Assert.Equal(new Money(7.19m, "EUR"), price);
    }

    [Fact]
    public void Express_outside_the_eu_pays_raised_fuel_and_customs()
    {
        var price = _calculator.QuoteParcel("ALDER", ServiceLevel.Express, TestData.Small, TestData.Oslo);

        Assert.Equal(new Money(22.05m, "EUR"), price);
    }

    [Fact]
    public void Oversize_adds_the_oversize_fee()
    {
        var normal = _calculator.ComputeSurcharges("ALDER", ServiceLevel.Economy, TestData.Small, TestData.Paris, new Money(10m, "EUR"));
        var oversize = _calculator.ComputeSurcharges("ALDER", ServiceLevel.Economy, TestData.Oversize, TestData.Paris, new Money(10m, "EUR"));

        Assert.Equal(18.50m, oversize.Amount - normal.Amount);
    }

    [Fact]
    public void Heavy_oversize_with_corvid_adds_the_heavy_fee()
    {
        var heavy = new Parcel(22_000, 130, 40, 30);

        var surcharges = _calculator.ComputeSurcharges("CORVID", ServiceLevel.Standard, heavy, TestData.Paris, new Money(10m, "EUR"));

        Assert.Equal(1.50m + 21.00m + 15.00m, surcharges.Amount);
    }

    [Fact]
    public void Corvid_refuses_dangerous_goods_overnight()
    {
        var dangerous = TestData.Small with { IsDangerousGoods = true };

        Assert.Throws<ShipmentRejectedException>(() =>
            _calculator.ComputeSurcharges("CORVID", ServiceLevel.Overnight, dangerous, TestData.Paris, new Money(10m, "EUR")));
    }

    [Fact]
    public void Insurance_is_charged_per_started_block()
    {
        Assert.Equal(new Money(5.00m, "EUR"), _calculator.InsuranceFor("ALDER", 501m));
        Assert.Equal(new Money(0m, "EUR"), _calculator.InsuranceFor("ALDER", 0m));
    }
}
