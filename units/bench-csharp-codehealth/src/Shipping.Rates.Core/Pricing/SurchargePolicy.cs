using Microsoft.Extensions.Options;
using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Pricing;

/// <summary>The carriers' published surcharges, passed on with our markup.</summary>
public sealed class SurchargePolicy
{
    private readonly SurchargeOptions _options;
    private readonly IReadOnlyDictionary<(string Carrier, string Kind), decimal> _table;

    public SurchargePolicy(IOptions<SurchargeOptions> options, IReadOnlyDictionary<(string Carrier, string Kind), decimal> table)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _table = table;
    }

    public static IReadOnlyDictionary<(string Carrier, string Kind), decimal> PublishedTable { get; } =
        new Dictionary<(string, string), decimal>
        {
            [("ALDER", "fuel")] = 0.12m,
            [("ALDER", "oversize")] = 18.50m,
            [("ALDER", "dangerous")] = 24.00m,
            [("ALDER", "remote")] = 9.90m,
            [("ALDER", "customs")] = 6.00m,
            [("ALDER", "heavy")] = 12.00m,
            [("ALDER", "insurance")] = 2.50m,
            [("CORVID", "fuel")] = 0.15m,
            [("CORVID", "oversize")] = 21.00m,
            [("CORVID", "dangerous")] = 32.00m,
            [("CORVID", "remote")] = 7.50m,
            [("CORVID", "customs")] = 4.50m,
            [("CORVID", "heavy")] = 15.00m,
            [("CORVID", "insurance")] = 3.00m,
        };

    public decimal FuelSurcharge(string carrier, decimal baseAmount)
    {
        var fuel = baseAmount * Rate(carrier, "fuel");
        return Math.Min(fuel, baseAmount * _options.MaxFuelShare) * _options.Markup;
    }

    public decimal OversizeFee(string carrier)
    {
        var fee = Rate(carrier, "oversize");
        return fee * _options.Markup;
    }

    public decimal HeavyFee(string carrier)
    {
        var fee = Rate(carrier, "heavy");
        return fee * _options.Markup;
    }

    public decimal DangerousGoodsFee(string carrier, Parcel parcel)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        var perStartedKg = Rate(carrier, "dangerous");
        return perStartedKg * Math.Ceiling(parcel.WeightGrams / 1000m) * _options.Markup;
    }

    public decimal RemoteAreaFee(string carrier)
    {
        var fee = Rate(carrier, "remote");
        return fee * _options.Markup;
    }

    public decimal CustomsFee(string carrier)
    {
        var fee = Rate(carrier, "customs");
        return fee * _options.Markup;
    }

    public decimal InsuranceFee(string carrier, decimal insuredValue)
    {
        if (insuredValue <= 0)
        {
            return 0m;
        }

        var blocks = Math.Ceiling(insuredValue / _options.InsuranceBlock);
        return blocks * Rate(carrier, "insurance") * _options.Markup;
    }

    public bool HasSurcharge(string carrier, string kind)
    {
        var key = (carrier, kind);
        return _table.ContainsKey(key);
    }

    private decimal Rate(string carrier, string kind)
    {
        var key = (carrier, kind);
        return _table.TryGetValue(key, out var rate)
            ? rate
            : throw new InvalidOperationException($"No {kind} surcharge is published for {carrier}.");
    }
}
