namespace ClinicScheduling.Application.Reminders;

public enum ReminderKind
{
    DayBefore,
    SameDay,
}

public sealed record PlannedReminder(Guid Id, Guid AppointmentId, Guid PatientId, DateTimeOffset SendAt, ReminderKind Kind);
