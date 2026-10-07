using ClinicScheduling.Domain.Recurrence;
using static ClinicScheduling.UnitTests.TestData;

namespace ClinicScheduling.UnitTests.Recurrence;

public sealed class RecurrenceExpanderTests
{
    private static readonly TimeOnly Nine = new(9, 0);

    [Fact]
    public void ExpandsWeeklySessionsFromTheFirstDay()
    {
        var rule = new RecurrenceRule(1, 4, [DayOfWeek.Monday, DayOfWeek.Thursday]);

        var sessions = new RecurrenceExpander(new FixedHolidays()).Expand(rule, Monday.AddDays(1), Nine, TimeSpan.FromMinutes(45), TimeZoneInfo.Utc, "DK");

        Assert.Equal(
            [At(Monday.AddDays(3), 9), At(Monday.AddDays(7), 9), At(Monday.AddDays(10), 9), At(Monday.AddDays(14), 9)],
            sessions.Select(s => s.Start));
        Assert.All(sessions, s => Assert.Equal(TimeSpan.FromMinutes(45), s.Duration));
    }

    [Fact]
    public void MovesAHolidaySessionToTheNextFreeWeekday()
    {
        var rule = new RecurrenceRule(1, 2, [DayOfWeek.Monday]);

        var sessions = new RecurrenceExpander(new FixedHolidays(Monday)).Expand(rule, Monday, Nine, TimeSpan.FromHours(1), TimeZoneInfo.Utc, "DK");

        Assert.Equal([At(Monday.AddDays(1), 9), At(Monday.AddDays(7), 9)], sessions.Select(s => s.Start));
    }

    [Fact]
    public void ExtendsTheSeriesWhenAWeekHasNoFreeDay()
    {
        var rule = new RecurrenceRule(2, 2, [DayOfWeek.Friday]);

        var sessions = new RecurrenceExpander(new FixedHolidays(Monday.AddDays(4))).Expand(rule, Monday, Nine, TimeSpan.FromHours(1), TimeZoneInfo.Utc, "DK");

        Assert.Equal([At(Monday.AddDays(18), 9), At(Monday.AddDays(32), 9)], sessions.Select(s => s.Start));
    }
}
