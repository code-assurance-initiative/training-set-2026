using Fx.Conversion.Currencies;

namespace Fx.Conversion.UnitTests.Currencies;

public sealed class CurrencyCatalogTests
{
    private readonly CurrencyCatalog _catalog = new();

    [Theory]
    [InlineData("EUR", 2)]
    [InlineData("JPY", 0)]
    [InlineData("ISK", 0)]
    [InlineData("GBP", 2)]
    public void KnowsTheMinorUnits(string code, int minorUnits)
    {
        Assert.Equal(minorUnits, _catalog.GetByCode(code).MinorUnits);
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("EURO")]
    [InlineData("XAU")]
    [InlineData("")]
    public void RejectsUnknownOrMalformedCodes(string code)
    {
        Assert.False(_catalog.TryGet(code, out _));
        var error = Assert.Throws<UnknownCurrencyException>(() => _catalog.GetByCode(code));
        Assert.Equal(code, error.Code);
    }

    [Fact]
    public void EveryCatalogCurrencyRoundTrips()
    {
        foreach (var currency in _catalog.All)
        {
            try
            {
                Assert.True(_catalog.TryGet(currency.Code, out var found));
                Assert.Same(currency, found);
            }
            catch (Exception ex)
            {
                Assert.Fail($"{currency.Code} does not round-trip through the catalog: {ex.Message}");
            }
        }
    }

    [Fact]
    public void ListsTheFeedCurrenciesAndTheEuro()
    {
        Assert.Equal(30, _catalog.All.Count);
        Assert.Contains(_catalog.All, currency => currency.Code == "EUR");
    }
}
