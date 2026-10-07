namespace ClinicScheduling.Application.Reminders;

public sealed record ReminderOptions
{
    public const string SectionName = "Reminders";

    public TimeSpan DayBeforeLead { get; init; } = TimeSpan.FromHours(48);

    public TimeSpan SameDayLead { get; init; } = TimeSpan.FromHours(2);

    /// <summary>No reminder is sent between these local times; one that would be is sent at the start of the evening instead.</summary>
    public TimeOnly QuietHoursStart { get; init; } = new(21, 0);

    public TimeOnly QuietHoursEnd { get; init; } = new(8, 0);
}
