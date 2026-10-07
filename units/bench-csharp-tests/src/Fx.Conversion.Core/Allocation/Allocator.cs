using Fx.Conversion.Monetary;

namespace Fx.Conversion.Allocation;

/// <summary>
/// Splits an amount into parts in proportion to weights without creating or losing a minor unit: the parts always
/// sum to the total. Remainders go to the parts with the largest fractional share (largest-remainder method), ties to
/// the earlier part.
/// </summary>
public static class Allocator
{
    public static IReadOnlyList<Money> Allocate(Money total, IReadOnlyList<int> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Count == 0)
        {
            throw new ArgumentException("At least one weight is required.", nameof(weights));
        }

        if (weights.Any(w => w < 0) || weights.Sum() == 0)
        {
            throw new ArgumentException("Weights must be non-negative and not all zero.", nameof(weights));
        }

        var unit = total.Currency.MinorUnit;
        var units = decimal.Truncate(total.Amount / unit);
        if (units * unit != total.Amount)
        {
            throw new ArgumentException($"{total} is not a whole number of minor units; round it first.", nameof(total));
        }

        decimal weightSum = weights.Sum();
        var shares = weights.Select(w => units * w / weightSum).ToArray();
        var allocated = shares.Select(decimal.Truncate).ToArray();
        var remainder = units - allocated.Sum();

        var order = Enumerable.Range(0, shares.Length)
            .OrderByDescending(i => Math.Abs(shares[i] - allocated[i]))
            .ThenBy(i => i)
            .ToArray();
        var step = Math.Sign(remainder);
        for (var k = 0; k < Math.Abs(remainder); k++)
        {
            allocated[order[k]] += step;
        }

        return [.. allocated.Select(u => new Money(u * unit, total.Currency))];
    }

    /// <summary>Splits <paramref name="total"/> into <paramref name="parts"/> equal-as-possible amounts.</summary>
    public static IReadOnlyList<Money> Split(Money total, int parts)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parts);
        return Allocate(total, Enumerable.Repeat(1, parts).ToArray());
    }
}
