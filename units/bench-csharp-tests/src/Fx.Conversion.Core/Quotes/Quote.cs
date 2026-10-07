using Fx.Conversion.Monetary;

namespace Fx.Conversion.Quotes;

/// <summary>
/// A firm offer: <see cref="Source"/> is debited, <see cref="Target"/> is credited, at <see cref="Rate"/>, for
/// <see cref="Fee"/> (in the source currency), valid until <see cref="ExpiresAt"/>.
/// </summary>
public sealed record Quote(Guid Id, Money Source, Money Target, decimal Rate, Money Fee, DateOnly RatesOf, DateTimeOffset ExpiresAt)
{
    public bool IsExpired(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return clock.GetUtcNow() >= ExpiresAt;
    }
}
