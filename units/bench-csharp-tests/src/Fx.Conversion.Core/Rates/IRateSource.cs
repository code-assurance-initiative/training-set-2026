namespace Fx.Conversion.Rates;

/// <summary>Where reference rates come from: the central bank feed, a cache in front of it, or a fixed table.</summary>
public interface IRateSource
{
    Task<RateTable> GetLatestAsync(CancellationToken cancellationToken);
}
