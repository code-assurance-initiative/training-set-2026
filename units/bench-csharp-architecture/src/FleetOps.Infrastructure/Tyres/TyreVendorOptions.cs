using System.ComponentModel.DataAnnotations;

namespace FleetOps.Infrastructure.Tyres;

public sealed class TyreVendorOptions
{
    public const string SectionName = "TyreVendor";

    [Required]
    public Uri? BaseAddress { get; set; }
}
