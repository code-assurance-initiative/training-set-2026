using FleetOps.Contracts.Maintenance;

namespace FleetOps.Infrastructure.Scheduling;

/// <summary>Spreads the due reminders over the next send window, one minute apart, so the relay is not flooded.</summary>
public sealed class MaintenanceReminderPlanner
{
    private readonly IReminderWindowCalculator _windows;

    public MaintenanceReminderPlanner(IReminderWindowCalculator windows)
    {
        _windows = windows;
    }

    public IReadOnlyList<PlannedReminder> Plan(IReadOnlyList<MaintenanceDue> due, DateTimeOffset now, QuietHours quietHours)
    {
        ArgumentNullException.ThrowIfNull(due);
        var window = _windows.NextWindow(now, quietHours);
        return
        [
            .. due
                .Select((d, index) => new PlannedReminder(d.VehicleId, d.Registration, d.Service, window.Start.AddMinutes(index)))
                .Where(p => p.SendAt < window.End),
        ];
    }
}
