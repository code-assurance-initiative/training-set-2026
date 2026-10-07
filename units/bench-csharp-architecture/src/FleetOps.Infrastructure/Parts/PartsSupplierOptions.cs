using System.ComponentModel.DataAnnotations;

namespace FleetOps.Infrastructure.Parts;

public sealed class PartsSupplierOptions
{
    public const string SectionName = "PartsSupplier";

    [Required]
    public Uri? BaseAddress { get; set; }
}
