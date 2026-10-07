using System.ComponentModel.DataAnnotations;
using FleetOps.Domain.Inspections;

namespace FleetOps.Api.Requests;

public sealed class RecordInspectionRequest
{
    [Required]
    public Guid VehicleId { get; init; }

    [Required]
    public IReadOnlyList<DefectRequest> Defects { get; init; } = [];
}

public sealed class DefectRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Description { get; init; } = string.Empty;

    [EnumDataType(typeof(DefectSeverity))]
    public DefectSeverity Severity { get; init; }
}
