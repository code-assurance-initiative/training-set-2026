using Microsoft.Extensions.Logging;

namespace Fx.Conversion.Rates;

/// <summary>
/// Keeps the latest rate table for a time-to-live, so requests do not each reach the feed. A background refresh can
/// keep it warm (<see cref="RefreshAsync"/>); a request that finds it empty or expired fetches once for all waiters.
/// </summary>
public sealed partial class RateCache(IRateSource inner, TimeProvider clock, TimeSpan timeToLive, ILogger<RateCache> logger)
    : IRateSource, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private (RateTable Table, DateTimeOffset FetchedAt)? _current;

    public bool TryGetCurrent(out RateTable? table)
    {
        var current = _current;
        if (current is { } entry && clock.GetUtcNow() - entry.FetchedAt < timeToLive)
        {
            table = entry.Table;
            return true;
        }

        table = null;
        return false;
    }

    public async Task<RateTable> GetLatestAsync(CancellationToken cancellationToken)
    {
        if (TryGetCurrent(out var cached) && cached is not null)
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return TryGetCurrent(out var fresh) && fresh is not null
                ? fresh
                : await FetchAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Fetches a new table regardless of the cached one's age.</summary>
    public async Task<RateTable> RefreshAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await FetchAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private async Task<RateTable> FetchAsync(CancellationToken cancellationToken)
    {
        var table = await inner.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        _current = (table, clock.GetUtcNow());
        LogRefreshed(table.PublishedOn, table.Rates.Count);
        return table;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Rate table of {PublishedOn} cached ({Count} rates)")]
    private partial void LogRefreshed(DateOnly publishedOn, int count);
}
