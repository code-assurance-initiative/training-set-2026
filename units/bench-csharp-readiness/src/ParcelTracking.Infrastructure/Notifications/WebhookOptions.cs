using System.ComponentModel.DataAnnotations;

namespace ParcelTracking.Infrastructure.Notifications;

public sealed class WebhookOptions
{
    public const string SectionName = "MerchantWebhooks";

    /// <summary>The merchant-hooks relay that fans events out to each merchant's registered endpoint.</summary>
    [Required]
    public Uri? BaseAddress { get; set; }

    /// <summary>Shared secret for the X-Signature-SHA256 header; supplied by the environment, never committed.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;
}
