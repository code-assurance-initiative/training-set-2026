namespace FleetOps.Infrastructure.FuelCards;

/// <summary>A fuel purchase imported from the fuel-card provider.</summary>
public sealed class FuelTransaction
{
    public Guid Id { get; set; }

    public string ProviderReference { get; set; } = string.Empty;

    public string Vin { get; set; } = string.Empty;

    public string Station { get; set; } = string.Empty;

    public decimal Litres { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset PurchasedAt { get; set; }
}
