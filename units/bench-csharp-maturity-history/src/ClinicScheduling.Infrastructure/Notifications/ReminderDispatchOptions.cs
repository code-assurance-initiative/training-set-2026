namespace ClinicScheduling.Infrastructure.Notifications;

public sealed class ReminderDispatchOptions
{
    public const string SectionName = "ReminderDispatch";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(30);

    public int BatchSize { get; set; } = 50;

    public string ClinicPhone { get; set; } = "+45 70 00 00 00";
}
