using System.ComponentModel.DataAnnotations;

namespace ParcelTracking.Worker;

/// <summary>How long data is kept; see docs/operations/data-retention.md.</summary>
public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    /// <summary>Delivered webhook notifications are kept this long after delivery.</summary>
    [Range(typeof(TimeSpan), "1.00:00:00", "365.00:00:00")]
    public TimeSpan DeliveredNotifications { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Delivered or returned parcels, with their history, are kept this long after their last update.</summary>
    [Range(typeof(TimeSpan), "30.00:00:00", "730.00:00:00")]
    public TimeSpan CompletedParcels { get; set; } = TimeSpan.FromDays(180);

    [Range(typeof(TimeSpan), "00:05:00", "1.00:00:00")]
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromHours(6);
}
