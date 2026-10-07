using System.ComponentModel.DataAnnotations;

namespace ParcelTracking.Worker;

public sealed class PollingOptions
{
    public const string SectionName = "Polling";

    /// <summary>Pause between two polling rounds.</summary>
    [Range(typeof(TimeSpan), "00:00:10", "01:00:00")]
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>A parcel is polled again once its last poll is older than this.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan PollAge { get; set; } = TimeSpan.FromMinutes(15);

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 200;

    [Range(typeof(TimeSpan), "00:00:05", "00:10:00")]
    public TimeSpan NotificationInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>File the liveness and readiness probes check for freshness.</summary>
    [Required]
    public string HeartbeatPath { get; set; } = "/tmp/heartbeat";
}
