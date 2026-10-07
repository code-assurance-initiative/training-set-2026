using System.ComponentModel.DataAnnotations;

namespace Quellbrook.Notifier.Retention;

/// <summary>How long the notifier keeps what it stores (docs/privacy.md).</summary>
public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    /// <summary>Contact details are kept this long after the delivery.</summary>
    [Range(typeof(TimeSpan), "1.00:00:00", "90.00:00:00")]
    public TimeSpan RecipientAfterDelivery { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Contact details of an order that never completed (cancelled, lost) are kept this long after it was announced.</summary>
    [Range(typeof(TimeSpan), "1.00:00:00", "180.00:00:00")]
    public TimeSpan RecipientWithoutDelivery { get; set; } = TimeSpan.FromDays(60);

    [Range(typeof(TimeSpan), "1.00:00:00", "365.00:00:00")]
    public TimeSpan NotificationLog { get; set; } = TimeSpan.FromDays(90);

    [Range(typeof(TimeSpan), "1.00:00:00", "90.00:00:00")]
    public TimeSpan ProcessedMessages { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
}
