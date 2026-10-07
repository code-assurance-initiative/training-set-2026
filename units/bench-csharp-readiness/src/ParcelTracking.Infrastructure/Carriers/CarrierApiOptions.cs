using System.ComponentModel.DataAnnotations;

namespace ParcelTracking.Infrastructure.Carriers;

public sealed class CarrierApiOptions
{
    public const string SectionName = "CarrierApi";

    [Required]
    public Uri? BaseAddress { get; set; }

    [Required]
    [MinLength(16)]
    public string ApiKey { get; set; } = string.Empty;

    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(5);

    [Range(typeof(TimeSpan), "00:00:05", "00:05:00")]
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(30);

    [Range(0, 10)]
    public int MaxRetryAttempts { get; set; } = 3;
}
