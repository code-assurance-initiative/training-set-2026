using System.ComponentModel.DataAnnotations;

namespace ReportDesk.Api.Feeds;

public sealed class FeedOptions
{
    public const string SectionName = "Feeds";

    /// <summary>The partner hosts whose records feeds may be pulled.</summary>
    [Required]
    [MinLength(1)]
    public string[] AllowedHosts { get; set; } = [];
}
