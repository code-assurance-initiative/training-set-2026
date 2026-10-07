using Fx.Conversion.Currencies;

namespace Fx.Conversion.UnitTests.Currencies;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData(0, "1")]
    [InlineData(1, "0.1")]
    [InlineData(2, "0.01")]
    [InlineData(3, "0.001")]
    [InlineData(4, "0.0001")]
    public void MinorUnitFollowsTheNumberOfDigits(int digits, string expected)
    {
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), new Currency("XTS", digits).MinorUnit);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void RejectsImpossibleMinorUnits(int digits)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Currency("XTS", digits));
    }

    [Fact]
    public void PrintsAsItsCode()
    {
        Assert.Equal("CHF", new Currency("CHF", 2).ToString());
    }
}
