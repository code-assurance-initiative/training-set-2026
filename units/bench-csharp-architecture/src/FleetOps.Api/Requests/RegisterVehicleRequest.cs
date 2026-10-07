using System.ComponentModel.DataAnnotations;

namespace FleetOps.Api.Requests;

public sealed class RegisterVehicleRequest
{
    [Required]
    [StringLength(17, MinimumLength = 17)]
    public string Vin { get; init; } = string.Empty;

    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string Registration { get; init; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Model { get; init; } = string.Empty;

    [Range(0, 2_000_000)]
    public int OdometerKm { get; init; }
}
