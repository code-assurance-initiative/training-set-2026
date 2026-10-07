namespace FleetOps.Infrastructure.Scheduling;

public sealed record ReminderWindow(DateTimeOffset Start, DateTimeOffset End);

public sealed record PlannedReminder(Guid VehicleId, string Registration, string Service, DateTimeOffset SendAt);

public sealed record ReminderRun(DateTimeOffset StartedAt, int Due, int Sent, int Deferred);
