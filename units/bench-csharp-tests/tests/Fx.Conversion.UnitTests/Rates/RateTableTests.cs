using Fx.Conversion.Currencies;
using Fx.Conversion.Rates;
using Fx.Conversion.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace Fx.Conversion.UnitTests.Rates;

public sealed class RateTableTests
{
    private static readonly RateTable Table = RateTables.Typical();

    [Fact]
    public void QuotesTheBaseCurrencyAtOne()
    {
        Assert.Equal(1m, Table.RateFor("EUR", "EUR"));
        Assert.True(Table.Quotes("EUR"));
    }

    [Fact]
    public void ConvertsFromTheBaseDirectly()
    {
        Assert.Equal(1.10m, Table.RateFor("EUR", "USD"));
        Assert.Equal(1m / 1.10m, Table.RateFor("USD", "EUR"));
    }

    [Fact]
    public void DerivesCrossRatesThroughTheBase()
    {
        Assert.Equal(0.85m / 1.10m, Table.RateFor("USD", "GBP"));
    }

    [Fact]
    public void CrossRateAndItsInverseMultiplyToOne()
    {
        Assert.Equal(1m, Table.RateFor("USD", "GBP") * Table.RateFor("GBP", "USD"));
    }

    [Fact]
    public void RejectsACurrencyItDoesNotQuote()
    {
        var error = Assert.Throws<UnknownCurrencyException>(() => Table.RateFor("EUR", "KWD"));
        Assert.Equal("KWD", error.Code);
    }

    [Fact]
    public void RejectsNonPositiveRates()
    {
        Assert.Throws<ArgumentException>(() => new RateTable("EUR", new DateOnly(2026, 3, 2), new Dictionary<string, decimal> { ["USD"] = 0m }));
    }

    [Fact]
    public void AgeCountsUtcDaysSincePublication()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 3, 4, 23, 30, 0, TimeSpan.Zero));

        Assert.Equal(2, Table.AgeInDays(clock));
    }

    [Fact]
    public void TablePublishedTodayIsCurrent()
    {
        var table = RateTables.Typical(DateOnly.FromDateTime(DateTime.Today));

        Assert.Equal(0, table.AgeInDays(TimeProvider.System));
    }
}
