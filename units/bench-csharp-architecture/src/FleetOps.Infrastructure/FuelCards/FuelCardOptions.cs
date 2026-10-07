using System.ComponentModel.DataAnnotations;

namespace FleetOps.Infrastructure.FuelCards;

public sealed class FuelCardOptions
{
    public const string SectionName = "FuelCards";

    [Required]
    public Uri? BaseAddress { get; set; }

    [Range(1, 5)]
    public int MaxAttempts { get; set; } = 3;
}
