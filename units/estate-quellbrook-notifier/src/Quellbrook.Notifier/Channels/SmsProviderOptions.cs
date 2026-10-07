using System.ComponentModel.DataAnnotations;

namespace Quellbrook.Notifier.Channels;

/// <summary>The SMS gateway.</summary>
public sealed class SmsProviderOptions
{
    public const string SectionName = "SmsProvider";

    [Required]
    public Uri BaseAddress { get; set; } = new("https://api.sms-gateway.example/");

    /// <summary>The gateway's API key; supplied by the environment, never written in configuration files.</summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The alphanumeric sender name shown on the phone.</summary>
    [Required]
    [StringLength(11)]
    public string Sender { get; set; } = "Quellbrook";
}
