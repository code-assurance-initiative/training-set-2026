using System.Net;
using Microsoft.Extensions.Time.Testing;
using Shipping.Rates.Core.Pricing;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Pricing;

public sealed class RateCardCacheTests
{
    private const string AlderCard = """{"carrier":"ALDER","currency":"EUR","baseFee":4.0,"perKgByZone":{"1":1.0,"2":1.5},"volumetricDivisor":5000}""";

    private readonly FakeTimeProvider _clock = new(TestData.Now);

    private RateCardCache Cache(StubHandler handler) =>
        new(handler.Client(), [new RateCardSource("ALDER", new Uri("https://rates.test/alder.json"))], _clock, Loggers.For<RateCardCache>());

    [Fact]
    public void A_card_put_into_the_cache_is_served_until_it_is_stale()
    {
        using var cache = Cache(new StubHandler(HttpStatusCode.OK, AlderCard));
        cache.Put(TestData.Card("ALDER"));

        Assert.Equal(4.00m, cache.GetCard("ALDER").BaseFee);

        _clock.Advance(TimeSpan.FromMinutes(31));
        Assert.Throws<RateCardUnavailableException>(() => cache.GetCard("ALDER"));
        Assert.Equal(1, cache.EvictStale());
        Assert.Equal(0, cache.EvictStale());
    }

    [Fact]
    public void An_unknown_carrier_has_no_card()
    {
        using var cache = Cache(new StubHandler(HttpStatusCode.OK, AlderCard));

        Assert.Throws<RateCardUnavailableException>(() => cache.GetCard("CORVID"));
    }

    [Fact]
    public async Task Warm_up_downloads_every_configured_card()
    {
        var handler = new StubHandler(HttpStatusCode.OK, AlderCard);
        using var cache = Cache(handler);

        cache.WarmUp();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (handler.Requests.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Equal(1.5m, cache.GetCard("ALDER").PerKgFor(2));
    }
}
