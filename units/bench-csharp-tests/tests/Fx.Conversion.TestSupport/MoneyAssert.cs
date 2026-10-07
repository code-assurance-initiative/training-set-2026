using Fx.Conversion.Monetary;
using Xunit;

namespace Fx.Conversion.TestSupport;

/// <summary>Assertions on <see cref="Money"/> that name both amount and currency on failure.</summary>
public static class MoneyAssert
{
    public static void Equal(decimal expectedAmount, string expectedCurrency, Money actual)
    {
        Assert.Equal(expectedCurrency, actual.Currency.Code);
        Assert.Equal(expectedAmount, actual.Amount);
    }
}
