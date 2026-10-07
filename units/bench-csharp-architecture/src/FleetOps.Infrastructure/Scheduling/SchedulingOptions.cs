using System.ComponentModel.DataAnnotations;

namespace FleetOps.Infrastructure.Scheduling;

public sealed class SchedulingOptions
{
    public const string SectionName = "Scheduling";

    [Required]
    [EmailAddress]
    public string ReminderRecipient { get; set; } = string.Empty;

    public TimeOnly QuietHoursStart { get; set; } = new(20, 0);

    public TimeOnly QuietHoursEnd { get; set; } = new(7, 0);

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan RunEvery { get; set; } = TimeSpan.FromHours(1);
}
