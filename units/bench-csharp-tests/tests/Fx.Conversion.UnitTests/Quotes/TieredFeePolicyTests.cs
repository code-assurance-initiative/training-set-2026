using Fx.Conversion.Monetary;
using Fx.Conversion.Quotes;
using Fx.Conversion.TestSupport;

namespace Fx.Conversion.UnitTests.Quotes;

public sealed class TieredFeePolicyTests
{
    private readonly TieredFeePolicy _policy = new();

    [Theory]
    [InlineData("1000", "5.00")]
    [InlineData("100", "1.00")]
    [InlineData("150", "1.00")]
    [InlineData("99999.99", "500.00")]
    [InlineData("100000", "250.00")]
    public void ChargesByTierWithAMinimum(string amount, string fee)
    {
        var charged = _policy.FeeFor(new Money(Parse(amount), KnownCurrencies.Eur));

        MoneyAssert.Equal(Parse(fee), "EUR", charged);
    }

    [Fact]
    public void TheMinimumIsOneHundredMinorUnits()
    {
        MoneyAssert.Equal(100m, "JPY", _policy.FeeFor(new Money(1_000m, KnownCurrencies.Jpy)));
    }

    private static decimal Parse(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
