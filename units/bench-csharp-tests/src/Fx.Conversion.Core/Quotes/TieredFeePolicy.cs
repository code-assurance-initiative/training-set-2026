using Fx.Conversion.Monetary;
using Fx.Conversion.Rounding;

namespace Fx.Conversion.Quotes;

/// <summary>
/// 0.50 % of the amount, at least 100 minor units (1.00 EUR, 100 JPY), and 0.25 % on amounts of 100,000 or more.
/// </summary>
public sealed class TieredFeePolicy : IFeePolicy
{
    private const decimal StandardRate = 0.005m;
    private const decimal VolumeRate = 0.0025m;
    private const decimal VolumeThreshold = 100_000m;
    private const int MinimumMinorUnits = 100;

    public Money FeeFor(Money source)
    {
        var amount = Math.Abs(source.Amount);
        var rate = amount >= VolumeThreshold ? VolumeRate : StandardRate;
        var minimum = MinimumMinorUnits * source.Currency.MinorUnit;
        var fee = Math.Max(amount * rate, minimum);
        return MoneyRounding.Round(new Money(fee, source.Currency), RoundingMode.HalfUp);
    }
}
