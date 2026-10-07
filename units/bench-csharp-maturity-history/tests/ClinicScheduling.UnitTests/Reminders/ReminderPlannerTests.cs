using ClinicScheduling.Application.Reminders;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Availability;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using static ClinicScheduling.UnitTests.TestData;

namespace ClinicScheduling.UnitTests.Reminders;

public sealed class ReminderPlannerTests
{
    private readonly ReminderPlanner _planner = new(Options.Create(new ReminderOptions()), new FakeTimeProvider(Now));

    private static Appointment At(DateTimeOffset start) =>
        Appointment.Book(Guid.NewGuid(), PractitionerId, Guid.NewGuid(), new TimeRange(start, start.AddHours(1)), false);

    [Fact]
    public void PlansADayBeforeAndASameDayReminder()
    {
        var appointment = At(TestData.At(Monday.AddDays(3), 14));

        var plan = _planner.Plan(appointment, TimeZoneInfo.Utc);

        Assert.Equal(
            [(ReminderKind.DayBefore, TestData.At(Monday.AddDays(1), 14)), (ReminderKind.SameDay, TestData.At(Monday.AddDays(3), 12))],
            plan.Select(r => (r.Kind, r.SendAt)));
    }

    [Fact]
    public void MovesAReminderOutOfTheQuietHours()
    {
        var appointment = At(TestData.At(Monday.AddDays(3), 9));

        var plan = _planner.Plan(appointment, TimeZoneInfo.Utc);

        Assert.Equal(TestData.At(Monday.AddDays(2), 20, 30), plan.Single(r => r.Kind == ReminderKind.SameDay).SendAt);
    }

    [Fact]
    public void SkipsRemindersThatAreAlreadyDue()
    {
        var appointment = At(Now.AddHours(5));

        var plan = _planner.Plan(appointment, TimeZoneInfo.Utc);

        Assert.Equal([ReminderKind.SameDay], plan.Select(r => r.Kind));
    }
}
