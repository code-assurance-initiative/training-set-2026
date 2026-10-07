using System.ComponentModel.DataAnnotations;

namespace ReportDesk.Api.Delivery;

public sealed class DeliveryOptions
{
    public const string SectionName = "Delivery";

    [Required]
    public Uri? CrmBaseAddress { get; set; }

    [Required]
    [EmailAddress]
    public string SenderAddress { get; set; } = string.Empty;

    [Required]
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>Key for the HMAC that pseudonymises addresses in logs; supplied by the platform's secret store.</summary>
    [Required]
    [MinLength(32)]
    public string PseudonymKey { get; set; } = string.Empty;
}
