using System.ComponentModel.DataAnnotations;

namespace FleetOps.Api.Requests;

public sealed class OpenWorkOrderRequest
{
    [Required]
    public Guid VehicleId { get; init; }

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;
}
