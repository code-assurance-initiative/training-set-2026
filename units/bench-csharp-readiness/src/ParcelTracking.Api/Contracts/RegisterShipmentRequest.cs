using System.ComponentModel.DataAnnotations;

namespace ParcelTracking.Api.Contracts;

public sealed class RegisterShipmentRequest
{
    [Required]
    [StringLength(40, MinimumLength = 8)]
    [RegularExpression("^[A-Z0-9]+$", ErrorMessage = "Tracking numbers are upper-case letters and digits.")]
    public string TrackingNumber { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^[A-Z]{3,16}$", ErrorMessage = "Carrier codes are 3 to 16 upper-case letters.")]
    public string CarrierCode { get; init; } = string.Empty;

    [Required]
    [StringLength(12, MinimumLength = 3)]
    [RegularExpression("^[A-Z0-9 -]+$", ErrorMessage = "Postal codes are letters, digits, spaces and hyphens.")]
    public string DestinationPostalCode { get; init; } = string.Empty;
}
