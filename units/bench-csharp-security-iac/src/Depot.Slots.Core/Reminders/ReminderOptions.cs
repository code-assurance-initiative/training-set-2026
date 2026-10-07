using System.ComponentModel.DataAnnotations;

namespace Depot.Slots.Core.Reminders;

public sealed class ReminderOptions
{
    public const string SectionName = "Reminders";

    /// <summary>How long before a slot starts the yard team is told about it.</summary>
    [Range(typeof(TimeSpan), "00:05:00", "04:00:00")]
    public TimeSpan LeadTime { get; set; } = TimeSpan.FromMinutes(30);

    [Range(typeof(TimeSpan), "00:00:10", "00:10:00")]
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(1);
}
