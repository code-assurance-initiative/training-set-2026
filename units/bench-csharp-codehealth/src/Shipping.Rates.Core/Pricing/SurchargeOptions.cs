namespace Shipping.Rates.Core.Pricing;

/// <summary>Surcharge settings that are ours rather than the carriers' (bound from configuration).</summary>
public sealed class SurchargeOptions
{
    public const string SectionName = "Surcharges";

    /// <summary>Multiplier applied to every carrier fee before it is passed on (1.0 = at cost).</summary>
    public decimal Markup { get; set; } = 1.0m;

    /// <summary>Fuel surcharges are capped at this share of the base price.</summary>
    public decimal MaxFuelShare { get; set; } = 0.25m;

    /// <summary>Insurance is charged per started block of this declared value.</summary>
    public decimal InsuranceBlock { get; set; } = 500m;
}
