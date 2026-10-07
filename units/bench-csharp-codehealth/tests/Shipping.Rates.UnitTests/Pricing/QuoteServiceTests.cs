using System.Net;
using Shipping.Rates.Core.Accounts;
using Shipping.Rates.Core.Carriers;
using Shipping.Rates.Core.Domain;
using Shipping.Rates.Core.Pricing;
using Shipping.Rates.UnitTests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace Shipping.Rates.UnitTests.Pricing;

public sealed class QuoteServiceTests
{
    private const string AlderRates = """{"rates":[{"service":"standard","amount":5.10,"currency":"EUR","transitDays":2},{"service":"express","amount":9.00,"currency":"EUR","transitDays":1}]}""";
    private const string CorvidRates = """{"rates":[{"service":"STD","amount":4.90,"currency":"EUR","transit":"3"}]}""";

    private static QuoteService Service(CarrierAccountManager? accounts = null)
    {
        var alder = new AlderParcelAdapter(new StubHandler(HttpStatusCode.OK, AlderRates).Client(), Loggers.For<AlderParcelAdapter>());
        var corvid = new CorvidCourierAdapter(new StubHandler(HttpStatusCode.OK, CorvidRates).Client(), Loggers.For<CorvidCourierAdapter>());
        var registry = new CarrierRegistry([alder, corvid], Loggers.For<CarrierRegistry>());
        return new QuoteService(
            registry,
            new MultiParcelQuoter(TestData.Calculator(), TestData.Policy()),
            accounts ?? new CarrierAccountManager(100, 100, [1, 2, 3]),
            new FakeTimeProvider(TestData.Now),
            Loggers.For<QuoteService>());
    }

    private static QuoteRequest Request(decimal insured = 0m) =>
        new(TestData.Berlin, TestData.Paris, [TestData.Small], ServiceLevel.Standard, new DateOnly(2026, 3, 10), insured);

    [Fact]
    public async Task Every_service_of_every_carrier_is_priced_cheapest_first()
    {
        var quotes = await Service().QuoteAsync(Request(), TestContext.Current.CancellationToken);

        Assert.Equal(3, quotes.Count);
        Assert.Equal(("ALDER", ServiceLevel.Standard, 7.00m, 2), (quotes[0].Carrier, quotes[0].Level, quotes[0].Total.Amount, quotes[0].TransitDays));
        Assert.Equal("CORVID", quotes[1].Carrier);
        Assert.Equal(3, quotes[1].TransitDays);
        Assert.Equal(ServiceLevel.Express, quotes[2].Level);
    }

    [Fact]
    public async Task A_carrier_over_its_rate_limit_is_skipped()
    {
        var accounts = new CarrierAccountManager(0, 10, [1]);

        var quotes = await Service(accounts).QuoteAsync(Request(), TestContext.Current.CancellationToken);

        Assert.Empty(quotes);
    }

    [Fact]
    public async Task Quotes_are_audited_per_carrier()
    {
        var accounts = new CarrierAccountManager(100, 10, [1]);

        await Service(accounts).QuoteAsync(Request(), TestContext.Current.CancellationToken);

        Assert.Equal(2, accounts.RecentAudit(10).Count);
    }
}
