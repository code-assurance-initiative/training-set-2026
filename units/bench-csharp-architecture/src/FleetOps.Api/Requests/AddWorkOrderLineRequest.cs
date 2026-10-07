using System.ComponentModel.DataAnnotations;
using FleetOps.Domain.WorkOrders;

namespace FleetOps.Api.Requests;

public sealed class AddWorkOrderLineRequest
{
    [Required]
    [EnumDataType(typeof(LineKind))]
    public LineKind Kind { get; init; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Description { get; init; } = string.Empty;

    [Range(0.01, 10_000)]
    public decimal Quantity { get; init; }

    [Range(0, 1_000_000)]
    public decimal UnitPrice { get; init; }
}
