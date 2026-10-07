using System.ComponentModel.DataAnnotations;

namespace Rentals.Billing.Application;

public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "EUR";

    /// <summary>The share of an item's replacement value held as a deposit while it is on loan.</summary>
    [Range(0.0, 1.0)]
    public decimal DepositShare { get; set; } = 0.2m;

    /// <summary>The late fee per day, as a multiple of the item's daily rate.</summary>
    [Range(0.0, 10.0)]
    public decimal LateFeeRateMultiple { get; set; } = 1.5m;

    [Range(0.0, 1000.0)]
    public decimal ExtensionFee { get; set; } = 2m;

    [Required]
    public Uri CatalogueBaseAddress { get; set; } = new("http://catalogue.internal/");

    [Required]
    public string StatementsEndpoint { get; set; } = "billing-statements";
}
