using System.ComponentModel.DataAnnotations;

namespace FleetOps.Infrastructure.Geocoding;

public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    [Required]
    public Uri? BaseAddress { get; set; }

    [Range(0.0, 1.0)]
    public double MinimumConfidence { get; set; } = 0.6;
}
