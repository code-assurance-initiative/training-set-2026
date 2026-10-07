using System.ComponentModel.DataAnnotations;

namespace Invoicing.Api.Branding;

/// <summary>Where tenant logos are fetched from: the vendor's internal branding service.</summary>
public sealed class BrandingOptions
{
    public const string SectionName = "Branding";

    [Required]
    public Uri? BaseAddress { get; set; }

    [Range(1, 30)]
    public int TimeoutSeconds { get; set; } = 5;

    [StringLength(200)]
    public string FooterText { get; set; } = string.Empty;
}
