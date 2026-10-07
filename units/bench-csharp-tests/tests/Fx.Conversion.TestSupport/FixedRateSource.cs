using Fx.Conversion.Rates;

namespace Fx.Conversion.TestSupport;

/// <summary>An <see cref="IRateSource"/> that always answers with the same table and counts how often it was asked.</summary>
public sealed class FixedRateSource(RateTable table) : IRateSource
{
    private int _calls;

    /// <summary>The typical table, published on the clock's current UTC date.</summary>
    public FixedRateSource(TimeProvider clock)
        : this(RateTables.Typical(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime)))
    {
    }

    public int Calls => Volatile.Read(ref _calls);

    public Task<RateTable> GetLatestAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _calls);
        return Task.FromResult(table);
    }
}
