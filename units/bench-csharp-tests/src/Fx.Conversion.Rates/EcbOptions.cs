using System.ComponentModel.DataAnnotations;

namespace Fx.Conversion.Rates.Ecb;

public sealed class EcbOptions
{
    public const string SectionName = "Ecb";

    /// <summary>The daily reference-rate document.</summary>
    [Required]
    public Uri FeedUri { get; set; } = new(EcbRateSource.DefaultFeed);

    /// <summary>How often the background refresh fetches the feed. The ECB publishes once per working day.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How long a fetched table is served before a request fetches again.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan CacheTimeToLive { get; set; } = TimeSpan.FromHours(2);
}
