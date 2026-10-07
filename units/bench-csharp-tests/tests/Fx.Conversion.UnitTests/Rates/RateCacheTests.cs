using Fx.Conversion.Rates;
using Fx.Conversion.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Fx.Conversion.UnitTests.Rates;

public sealed class RateCacheTests
{
    private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(30);

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 2, 15, 0, 0, TimeSpan.Zero));
    private readonly FixedRateSource _origin = new(RateTables.Typical());

    [Fact]
    public async Task EntryExpiresAfterItsTimeToLive()
    {
        using var cache = CreateCache();
        await cache.GetLatestAsync(TestContext.Current.CancellationToken);

        _clock.Advance(TimeToLive - TimeSpan.FromSeconds(1));
        Assert.True(cache.TryGetCurrent(out _));

        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(cache.TryGetCurrent(out _));
    }

    [Fact]
    public async Task ServesRepeatedRequestsFromOneFetch()
    {
        using var cache = CreateCache();

        var first = await cache.GetLatestAsync(TestContext.Current.CancellationToken);
        var second = await cache.GetLatestAsync(TestContext.Current.CancellationToken);

        Assert.Same(first, second);
        Assert.Equal(1, _origin.Calls);
    }

    [Fact]
    public async Task FetchesAgainOnceExpired()
    {
        using var cache = CreateCache();
        await cache.GetLatestAsync(TestContext.Current.CancellationToken);

        _clock.Advance(TimeToLive);
        await cache.GetLatestAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, _origin.Calls);
    }

    [Fact]
    public async Task RefreshAlwaysFetches()
    {
        using var cache = CreateCache();
        await cache.GetLatestAsync(TestContext.Current.CancellationToken);

        await cache.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, _origin.Calls);
        Assert.True(cache.TryGetCurrent(out _));
    }

    [Fact]
    public void IsEmptyBeforeTheFirstFetch()
    {
        using var cache = CreateCache();

        Assert.False(cache.TryGetCurrent(out var table));
        Assert.Null(table);
    }

    private RateCache CreateCache() => new(_origin, _clock, TimeToLive, NullLogger<RateCache>.Instance);
}
