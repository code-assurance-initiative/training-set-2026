using System.Diagnostics;
using Fx.Conversion.Rates;
using Fx.Conversion.Rates.Ecb;
using Fx.Conversion.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Fx.Conversion.UnitTests.Rates;

public sealed class RateRefreshServiceTests : IDisposable
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 2, 15, 0, 0, TimeSpan.Zero));
    private readonly FixedRateSource _origin = new(RateTables.Typical());
    private readonly RateCache _cache;
    private readonly RateRefreshService _service;

    public RateRefreshServiceTests()
    {
        _cache = new RateCache(_origin, _clock, TimeSpan.FromHours(2), NullLogger<RateCache>.Instance);
        _service = new RateRefreshService(_cache, _clock, Options.Create(new EcbOptions()), NullLogger<RateRefreshService>.Instance);
    }

    [Fact]
    public async Task RefreshPopulatesTheCache()
    {
        _ = Task.Run(() => _service.StartAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        Thread.Sleep(250);

        Assert.True(_cache.TryGetCurrent(out var table));
        Assert.Equal("EUR", table?.BaseCurrency);
        await _service.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartedServiceFillsTheCache()
    {
        var starting = Task.Run(() => _service.StartAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);

        var deadline = Stopwatch.StartNew();
        while (!_cache.TryGetCurrent(out _) && deadline.Elapsed < TimeSpan.FromSeconds(5))
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        await starting;
        Assert.True(_cache.TryGetCurrent(out _));
        Assert.Equal(1, _origin.Calls);
        await _service.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RefreshesAgainOnEveryInterval()
    {
        await _service.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => _origin.Calls == 1);

        _clock.Advance(new EcbOptions().RefreshInterval);
        await WaitUntilAsync(() => _origin.Calls == 2);

        Assert.Equal(2, _origin.Calls);
        await _service.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task KeepsRunningWhenTheFeedFails()
    {
        var failing = new FailingSource();
        using var cache = new RateCache(failing, _clock, TimeSpan.FromHours(2), NullLogger<RateCache>.Instance);
        using var service = new RateRefreshService(cache, _clock, Options.Create(new EcbOptions()), NullLogger<RateRefreshService>.Instance);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => failing.Calls == 1);

        _clock.Advance(new EcbOptions().RefreshInterval);
        await WaitUntilAsync(() => failing.Calls == 2);

        Assert.Equal(2, failing.Calls);
        Assert.False(cache.TryGetCurrent(out _));
        await service.StopAsync(TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        _service.Dispose();
        _cache.Dispose();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = Stopwatch.StartNew();
        while (!condition() && deadline.Elapsed < TimeSpan.FromSeconds(5))
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    private sealed class FailingSource : IRateSource
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public Task<RateTable> GetLatestAsync(CancellationToken cancellationToken) =>
            Interlocked.Increment(ref _calls) % 2 == 1
                ? Task.FromException<RateTable>(new HttpRequestException("feed unavailable"))
                : Task.FromException<RateTable>(new FormatException("feed truncated"));
    }
}
