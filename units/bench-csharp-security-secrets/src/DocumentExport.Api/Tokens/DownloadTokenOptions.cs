using System.ComponentModel.DataAnnotations;

namespace DocumentExport.Api.Tokens;

/// <summary>Download-token settings (bound from the "Jwt" section).</summary>
public sealed class DownloadTokenOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(typeof(TimeSpan), "00:01:00", "01:00:00")]
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(10);
}
