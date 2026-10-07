using System.ComponentModel.DataAnnotations;

namespace FleetOps.Infrastructure.Telematics;

public sealed class TelematicsOptions
{
    public const string SectionName = "Telematics";

    [Required]
    public Uri? BaseAddress { get; set; }

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 10;
}
