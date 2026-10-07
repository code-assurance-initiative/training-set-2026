namespace HarbourLane.Bookings.Preferences;

public sealed record UserPreferences(bool EmailReminders, int ReminderHoursBefore, bool CompactCalendar)
{
    public static UserPreferences Default { get; } = new(EmailReminders: true, ReminderHoursBefore: 24, CompactCalendar: false);

    public static IReadOnlyList<int> ReminderChoices { get; } = [2, 24, 48];
}
