using FleetOps.Infrastructure.Scheduling;

namespace FleetOps.Application.Tests.Infrastructure;

public sealed class SchedulingTests
{
    private static readonly QuietHours Night = new(new TimeOnly(20, 0), new TimeOnly(7, 0));

    [Theory]
    [InlineData(21, true)]
    [InlineData(3, true)]
    [InlineData(7, false)]
    [InlineData(12, false)]
    public void QuietHoursWrapPastMidnight(int hour, bool quiet)
    {
        Assert.Equal(quiet, Night.Contains(new DateTimeOffset(2026, 5, 4, hour, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void AWindowAskedForDuringQuietHoursOpensWhenTheyEnd()
    {
        var window = new ReminderWindowCalculator().NextWindow(new DateTimeOffset(2026, 5, 4, 22, 30, 0, TimeSpan.Zero), Night);
        Assert.Equal(new DateTimeOffset(2026, 5, 5, 7, 0, 0, TimeSpan.Zero), window.Start);
        Assert.Equal(new DateTimeOffset(2026, 5, 5, 20, 0, 0, TimeSpan.Zero), window.End);
    }

    [Fact]
    public void RemindersAreSpreadOneMinuteApartInsideTheWindow()
    {
        var planner = new MaintenanceReminderPlanner(new ReminderWindowCalculator());
        var due = new[]
        {
            new Contracts.Maintenance.MaintenanceDue(Guid.NewGuid(), "AB12345", "Oil and filters", 15_000, 15_200),
            new Contracts.Maintenance.MaintenanceDue(Guid.NewGuid(), "CD67890", "Brake inspection", 30_000, 30_100),
        };

        var plan = planner.Plan(due, new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), Night);

        Assert.Equal([new TimeSpan(9, 0, 0), new TimeSpan(9, 1, 0)], plan.Select(p => p.SendAt.TimeOfDay));
    }
}
