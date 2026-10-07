using System.Globalization;
using Fx.Conversion.Monetary;
using Fx.Conversion.Rounding;
using Fx.Conversion.TestSupport;

namespace Fx.Conversion.UnitTests.Rounding;

public sealed class MoneyRoundingTests
{
    [Theory]
    [InlineData("2.345", RoundingMode.HalfEven, "2.34")]
    [InlineData("2.355", RoundingMode.HalfEven, "2.36")]
    [InlineData("2.345", RoundingMode.HalfUp, "2.35")]
    [InlineData("-2.345", RoundingMode.HalfUp, "-2.35")]
    [InlineData("2.349", RoundingMode.Down, "2.34")]
    [InlineData("-2.349", RoundingMode.Down, "-2.34")]
    [InlineData("2.341", RoundingMode.Up, "2.35")]
    [InlineData("-2.341", RoundingMode.Up, "-2.35")]
    [InlineData("-2.341", RoundingMode.Floor, "-2.35")]
    [InlineData("-2.349", RoundingMode.Ceiling, "-2.34")]
    public void RoundsToTwoDecimals(string amount, RoundingMode mode, string expected)
    {
        var rounded = MoneyRounding.Round(Parse(amount), 2, mode);

        Assert.Equal(Parse(expected), rounded);
    }

    [Fact]
    public void RoundsMoneyToItsCurrencyMinorUnit()
    {
        MoneyAssert.Equal(1234m, "JPY", MoneyRounding.Round(new Money(1234.5m, KnownCurrencies.Jpy), RoundingMode.HalfEven));
        MoneyAssert.Equal(1235m, "JPY", MoneyRounding.Round(new Money(1234.5m, KnownCurrencies.Jpy), RoundingMode.HalfUp));
        MoneyAssert.Equal(0.13m, "EUR", MoneyRounding.Round(new Money(0.125m, KnownCurrencies.Eur), RoundingMode.Up));
    }

    [Fact]
    public void LeavesExactAmountsAlone()
    {
        foreach (var mode in Enum.GetValues<RoundingMode>())
        {
            Assert.Equal(19.99m, MoneyRounding.Round(19.99m, 2, mode));
        }
    }

    [Fact]
    public void RejectsAnUndefinedMode()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MoneyRounding.Round(1m, 2, (RoundingMode)42));
    }

    private static decimal Parse(string value) => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
}
