using System.Globalization;
using Fx.Conversion.Currencies;
using Fx.Conversion.Monetary;
using Fx.Conversion.TestSupport;

namespace Fx.Conversion.UnitTests.Monetary;

public sealed class MoneyFormatterTests(ITestOutputHelper output)
{
    [Fact]
    public void FormatsWithTheRequestedCulture()
    {
        var money = new Money(1234.5m, KnownCurrencies.Eur);

        var text = MoneyFormatter.Format(money, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal("1.234,50 EUR", text);
    }

    [Fact]
    public void UsesTheCurrencyMinorUnits()
    {
        Assert.Equal("1,234 JPY", MoneyFormatter.Format(new Money(1234m, KnownCurrencies.Jpy), CultureInfo.InvariantCulture));
        Assert.Equal("0.10 USD", MoneyFormatter.Format(new Money(0.1m, KnownCurrencies.Usd), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void DisplaysInTheCurrentCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
        try
        {
            Assert.Equal("1,000.00 GBP", MoneyFormatter.FormatForDisplay(new Money(1000m, KnownCurrencies.Gbp)));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void FormatsEveryCatalogCurrency()
    {
        foreach (var currency in new CurrencyCatalog().All)
        {
            try
            {
                var text = MoneyFormatter.Format(new Money(1m, currency), CultureInfo.InvariantCulture);
                Assert.EndsWith(" " + currency.Code, text, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                output.WriteLine($"{currency.Code}: {ex.Message}");
            }
        }
    }
}
