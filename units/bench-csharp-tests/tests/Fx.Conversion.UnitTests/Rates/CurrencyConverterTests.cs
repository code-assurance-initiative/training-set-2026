using Fx.Conversion.Currencies;
using Fx.Conversion.Monetary;
using Fx.Conversion.Rates;
using Fx.Conversion.Rounding;
using Fx.Conversion.TestSupport;
using NSubstitute;

namespace Fx.Conversion.UnitTests.Rates;

public sealed class CurrencyConverterTests
{
    private readonly CurrencyCatalog _catalog = new();
    private readonly IRateSource _rates = Substitute.For<IRateSource>();

    public CurrencyConverterTests()
    {
        _rates.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(RateTables.Typical());
    }

    [Fact]
    public async Task ConvertsAtTheLatestRate()
    {
        var converter = new CurrencyConverter(_rates, _catalog);

        var result = await converter.ConvertAsync(new Money(100m, KnownCurrencies.Eur), "USD", RoundingMode.HalfEven, TestContext.Current.CancellationToken);

        MoneyAssert.Equal(110.00m, "USD", result);
        await _rates.Received(1).GetLatestAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RoundsToTheTargetMinorUnit()
    {
        var converter = new CurrencyConverter(_rates, _catalog);

        var result = await converter.ConvertAsync(new Money(10.03m, KnownCurrencies.Eur), "JPY", RoundingMode.HalfEven, TestContext.Current.CancellationToken);

        MoneyAssert.Equal(1605m, "JPY", result);
    }

    [Fact]
    public async Task ConvertsLargeAmountsWithoutOverflow()
    {
        var converter = new CurrencyConverter(_rates, _catalog);

        var result = await converter.ConvertAsync(new Money(950_000_000m, KnownCurrencies.Eur), "JPY", RoundingMode.HalfEven, TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "wip")]
    public async Task ConvertsBetweenTwoQuotedCurrencies()
    {
        var converter = new CurrencyConverter(_rates, _catalog);

        var result = await converter.ConvertAsync(new Money(110m, KnownCurrencies.Usd), "GBP", RoundingMode.HalfEven, TestContext.Current.CancellationToken);

        MoneyAssert.Equal(85.00m, "GBP", result);
    }

    [Fact]
    public async Task RejectsAnUnknownTarget()
    {
        var converter = new CurrencyConverter(_rates, _catalog);

        await Assert.ThrowsAsync<UnknownCurrencyException>(
            () => converter.ConvertAsync(new Money(1m, KnownCurrencies.Eur), "XAU", RoundingMode.HalfEven, TestContext.Current.CancellationToken));
    }
}
