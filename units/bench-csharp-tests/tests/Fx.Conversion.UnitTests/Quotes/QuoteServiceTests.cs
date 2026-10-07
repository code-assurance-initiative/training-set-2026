using Fx.Conversion.Currencies;
using Fx.Conversion.Monetary;
using Fx.Conversion.Quotes;
using Fx.Conversion.Rates;
using Fx.Conversion.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Fx.Conversion.UnitTests.Quotes;

public sealed class QuoteServiceTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 2, 15, 0, 0, TimeSpan.Zero));
    private readonly InMemoryQuoteStore _store = new();
    private readonly RecordingMetrics _metrics = new();

    [Fact]
    public async Task CreatingAQuoteCallsEveryCollaborator()
    {
        var rates = Substitute.For<IRateSource>();
        var catalog = Substitute.For<ICurrencyCatalog>();
        var fees = Substitute.For<IFeePolicy>();
        var store = Substitute.For<IQuoteStore>();
        var audit = Substitute.For<IQuoteAudit>();
        var metrics = Substitute.For<IConversionMetrics>();
        catalog.GetByCode("USD").Returns(KnownCurrencies.Usd);
        rates.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(RateTables.Typical());
        fees.FeeFor(Arg.Any<Money>()).Returns(new Money(1m, KnownCurrencies.Eur));
        var service = new QuoteService(rates, catalog, fees, store, audit, metrics, _clock);

        await service.CreateAsync(new Money(100m, KnownCurrencies.Eur), "USD", TestContext.Current.CancellationToken);

        catalog.Received(1).GetByCode("USD");
        await rates.Received(1).GetLatestAsync(Arg.Any<CancellationToken>());
        fees.Received(1).FeeFor(Arg.Any<Money>());
        await store.Received(1).SaveAsync(Arg.Any<Quote>(), Arg.Any<CancellationToken>());
        audit.Received(1).Issued(Arg.Any<Quote>());
        metrics.Received(1).QuoteIssued("EUR", "USD");
    }

    [Fact]
    public async Task QuotesTheNetAmountAtTheDayRate()
    {
        var quote = await CreateService().CreateAsync(new Money(1_000m, KnownCurrencies.Eur), "USD", TestContext.Current.CancellationToken);

        MoneyAssert.Equal(5.00m, "EUR", quote.Fee);
        MoneyAssert.Equal(1_094.50m, "USD", quote.Target);
        Assert.Equal(1.10m, quote.Rate);
        Assert.Equal(new DateOnly(2026, 3, 2), quote.RatesOf);
    }

    [Fact]
    public async Task RoundsTheCreditDown()
    {
        var quote = await CreateService().CreateAsync(new Money(123.45m, KnownCurrencies.Usd), "JPY", TestContext.Current.CancellationToken);

        MoneyAssert.Equal(17_810m, "JPY", quote.Target);
    }

    [Fact]
    public async Task StoresAuditsAndCountsTheQuote()
    {
        var quote = await CreateService().CreateAsync(new Money(500m, KnownCurrencies.Gbp), "EUR", TestContext.Current.CancellationToken);

        Assert.Same(quote, await _store.FindAsync(quote.Id, TestContext.Current.CancellationToken));
        Assert.Equal([("GBP", "EUR")], _metrics.Issued);
    }

    [Fact]
    public async Task AQuoteIsValidForNinetySeconds()
    {
        var service = CreateService();
        var quote = await service.CreateAsync(new Money(100m, KnownCurrencies.Eur), "GBP", TestContext.Current.CancellationToken);

        _clock.Advance(QuoteService.Validity - TimeSpan.FromSeconds(1));
        Assert.NotNull(await service.FindValidAsync(quote.Id, TestContext.Current.CancellationToken));

        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Null(await service.FindValidAsync(quote.Id, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("0.80")]
    public async Task RejectsAmountsThatCannotBeQuoted(string amount)
    {
        var source = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), KnownCurrencies.Eur);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => CreateService().CreateAsync(source, "USD", TestContext.Current.CancellationToken));
    }

    private QuoteService CreateService() =>
        new(
            new FixedRateSource(RateTables.Typical()),
            new CurrencyCatalog(),
            new TieredFeePolicy(),
            _store,
            new LoggingQuoteAudit(NullLogger<LoggingQuoteAudit>.Instance),
            _metrics,
            _clock);

    private sealed class RecordingMetrics : IConversionMetrics
    {
        public List<(string Source, string Target)> Issued { get; } = [];

        public void QuoteIssued(string sourceCurrency, string targetCurrency) => Issued.Add((sourceCurrency, targetCurrency));
    }
}
