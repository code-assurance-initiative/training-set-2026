using System.ComponentModel.DataAnnotations;

namespace ReportDesk.Api.Conversion;

public sealed class ConversionOptions
{
    public const string SectionName = "Conversion";

    [Required]
    public string SofficePath { get; set; } = "/usr/bin/soffice";

    [Required]
    public string ConvertPath { get; set; } = "/usr/bin/convert";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
}
