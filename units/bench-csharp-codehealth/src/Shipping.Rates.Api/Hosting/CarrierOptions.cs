using System.ComponentModel.DataAnnotations;

namespace Shipping.Rates.Api.Hosting;

/// <summary>Where each carrier's API lives and how to authenticate (bound from section <c>Carriers</c>).</summary>
public sealed class CarrierOptions
{
    public const string SectionName = "Carriers";

    [Required]
    public CarrierEndpoint Alder { get; set; } = new();

    [Required]
    public CarrierEndpoint Corvid { get; set; } = new();

    /// <summary>Rate card download URL per carrier code.</summary>
    public Dictionary<string, Uri> RateCards { get; set; } = new(StringComparer.Ordinal);

    public string? CustomAdapter { get; set; }
}

public sealed class CarrierEndpoint
{
    public Uri BaseAddress { get; set; } = new("https://localhost/");

    /// <summary>Supplied by the environment (e.g. <c>Carriers__Alder__ApiKey</c>); never committed.</summary>
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>Signature settings for inbound carrier webhooks (bound from section <c>Webhooks</c>).</summary>
public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks";

    /// <summary>Base64 HMAC key shared with the carriers; supplied by the environment.</summary>
    [Required]
    public string Secret { get; set; } = string.Empty;

    [Range(1, 10_000)]
    public int RequestsPerMinute { get; set; } = 600;

    [Range(10, 100_000)]
    public int AuditCapacity { get; set; } = 1_000;
}
