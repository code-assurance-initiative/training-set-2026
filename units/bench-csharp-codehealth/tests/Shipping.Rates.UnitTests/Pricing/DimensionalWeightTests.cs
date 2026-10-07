using Shipping.Rates.Core.Domain;
using Shipping.Rates.Core.Pricing;

namespace Shipping.Rates.UnitTests.Pricing;

public sealed class DimensionalWeightTests
{
    [Fact]
    public void A_light_bulky_parcel_is_charged_by_volume()
    {
        var pillow = new Parcel(500, 60, 40, 40);

        Assert.Equal(19_200, DimensionalWeight.ChargeableGrams(pillow, 5000));
    }

    [Fact]
    public void A_dense_parcel_is_charged_by_weight()
    {
        var books = new Parcel(8_000, 30, 20, 15);

        Assert.Equal(8_000, DimensionalWeight.ChargeableGrams(books, 5000));
    }

    [Theory]
    [InlineData(1, 0.5)]
    [InlineData(500, 0.5)]
    [InlineData(501, 1.0)]
    [InlineData(2_400, 2.5)]
    public void Kilograms_are_rounded_up_to_the_half(int grams, double expected)
    {
        Assert.Equal((decimal)expected, DimensionalWeight.ChargeableKilograms(grams));
    }

    [Fact]
    public void A_zero_divisor_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DimensionalWeight.VolumetricGrams(new Parcel(1, 1, 1, 1), 0));
    }
}
