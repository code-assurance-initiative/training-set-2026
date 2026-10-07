using System.ComponentModel.DataAnnotations;

namespace Quellbrook.Notifier.Channels;

/// <summary>The transactional e-mail provider (SendGrid's v3 API).</summary>
public sealed class EmailProviderOptions
{
    public const string SectionName = "EmailProvider";

    [Required]
    public Uri BaseAddress { get; set; } = new("https://api.sendgrid.com/");

    /// <summary>The provider's API key; supplied by the environment, never written in configuration files.</summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    [Required]
    public string FromName { get; set; } = "Quellbrook Freight";
}
