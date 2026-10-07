namespace FleetOps.Infrastructure.FuelCards;

/// <summary>The latest pump price per station, refreshed at most every fifteen minutes.</summary>
public sealed class FuelPriceCache
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(15);
    private readonly IFuelCardTransactionStore _store;
    private readonly TimeProvider _clock;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, FuelPrice> _prices = new(StringComparer.OrdinalIgnoreCase);

    public FuelPriceCache(IFuelCardTransactionStore store, TimeProvider clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<FuelPrice?> GetAsync(string station, CancellationToken cancellationToken)
    {
        if (_prices.TryGetValue(station, out var cached) && _clock.GetUtcNow() - cached.ObservedAt < MaxAge)
        {
            return cached;
        }

        var latest = await _store.LatestPriceAsync(station, cancellationToken).ConfigureAwait(false);
        if (latest is not null)
        {
            _prices[station] = latest with { ObservedAt = _clock.GetUtcNow() };
        }

        return latest;
    }
}
