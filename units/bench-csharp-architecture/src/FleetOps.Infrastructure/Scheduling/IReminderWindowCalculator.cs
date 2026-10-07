namespace FleetOps.Infrastructure.Scheduling;

public interface IReminderWindowCalculator
{
    ReminderWindow NextWindow(DateTimeOffset now, QuietHours quietHours);
}
