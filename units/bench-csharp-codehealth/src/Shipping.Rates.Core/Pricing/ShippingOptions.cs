namespace Shipping.Rates.Core.Pricing;

/// <summary>Operational settings of the shipping desk (bound from configuration).</summary>
public sealed class ShippingOptions
{
    public const string SectionName = "Shipping";

    /// <summary>Daily cut-off as HH:mm local time; parcels booked later ship the next working day.</summary>
    public string? DailyCutoff { get; set; }
}
