using System.ComponentModel.DataAnnotations;

namespace DocumentExport.Api.Partner;

/// <summary>The fulfilment partner's API (bound from the "Partner" section of partner-api.json).</summary>
public sealed class PartnerOptions
{
    public const string SectionName = "Partner";

    [Required]
    public Uri? BaseUrl { get; set; }

    public Dictionary<string, string> DefaultRequestHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [Range(typeof(TimeSpan), "00:00:01", "00:02:00")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
}
