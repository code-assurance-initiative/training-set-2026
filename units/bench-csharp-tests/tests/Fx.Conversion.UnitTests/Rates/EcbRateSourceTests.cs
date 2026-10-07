using System.Diagnostics;
using System.Net;
using Fx.Conversion.Rates.Ecb;
using Fx.Conversion.UnitTests.Fixtures;
using Fx.Conversion.UnitTests.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Fx.Conversion.UnitTests.Rates;

public sealed class EcbRateSourceTests
{
    [Fact]
    public async Task ParsesTheDailyFeed()
    {
        using var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Xml(Feeds.Daily));
        var source = CreateSource(handler, TimeProvider.System);

        var table = await source.GetLatestAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new DateOnly(2026, 3, 2), table.PublishedOn);
        Assert.Equal("https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml", Assert.Single(handler.Requests)?.AbsoluteUri);
    }

    [Fact]
    public async Task RetriesTransientFailures()
    {
        var clock = new FakeTimeProvider();
        var attempts = 0;
        using var handler = new StubHttpMessageHandler(_ => Interlocked.Increment(ref attempts) <= 2
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : StubHttpMessageHandler.Xml(Feeds.Daily));
        var source = CreateSource(handler, clock);
        var started = clock.GetUtcNow();

        var fetch = source.GetLatestAsync(TestContext.Current.CancellationToken);
        var watchdog = Stopwatch.StartNew();
        while (!fetch.IsCompleted && watchdog.Elapsed < TimeSpan.FromSeconds(10))
        {
            clock.Advance(TimeSpan.FromMilliseconds(100));
            await Task.Yield();
        }

        var table = await fetch.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(new DateOnly(2026, 3, 2), table.PublishedOn);
        Assert.Equal(3, attempts);
        Assert.True(clock.GetUtcNow() - started >= EcbResilience.BaseDelay * 3, "two back-offs: 1 s, then 2 s");
    }

    [Fact]
    public async Task GivesUpOnAClientError()
    {
        using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var source = CreateSource(handler, new FakeTimeProvider());

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => source.GetLatestAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task LiveFeedParses()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        string xml;
        try
        {
            xml = await http.GetStringAsync("https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml", TestContext.Current.CancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Assert.Skip($"ECB feed unreachable: {ex.Message}");
            return;
        }

        var table = EcbXmlParser.Parse(xml);

        Assert.Equal("EUR", table.BaseCurrency);
        Assert.True(table.Quotes("USD"));
    }

    private static EcbRateSource CreateSource(HttpMessageHandler handler, TimeProvider clock) =>
        new(
            new SingleClientFactory(handler),
            EcbResilience.Create(clock),
            Options.Create(new EcbOptions()),
            NullLogger<EcbRateSource>.Instance);
}
