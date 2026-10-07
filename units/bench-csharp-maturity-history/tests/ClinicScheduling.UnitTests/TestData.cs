using ClinicScheduling.Domain.Availability;
using ClinicScheduling.Domain.Holidays;

namespace ClinicScheduling.UnitTests;

/// <summary>A holiday calendar that knows only the days it is given.</summary>
public sealed class FixedHolidays(params DateOnly[] days) : IHolidayCalendar
{
    public bool IsHoliday(string countryCode, DateOnly day) => days.Contains(day);
}

public static class TestData
{
    /// <summary>Monday 2 March 2026, 07:00 UTC.</summary>
    public static readonly DateTimeOffset Now = new(2026, 3, 2, 7, 0, 0, TimeSpan.Zero);

    public static readonly DateOnly Monday = new(2026, 3, 2);

    public static readonly Guid PractitionerId = Guid.Parse("6f1d2c7e-5a43-4b8e-9c11-0d2f3a4b5c6d");

    public static PractitionerSchedule Schedule(
        IEnumerable<WeeklyHours>? hours = null,
        IEnumerable<string>? skills = null,
        bool telehealth = true,
        int maxPerDay = 12) =>
        new(
            PractitionerId,
            "DK",
            TimeZoneInfo.Utc,
            hours ?? Weekdays(new TimeOnly(8, 0), new TimeOnly(16, 0)),
            skills ?? ["sports"],
            telehealth,
            maxPerDay);

    public static IEnumerable<WeeklyHours> Weekdays(TimeOnly opens, TimeOnly closes, TimeOnly? breakStarts = null, TimeOnly? breakEnds = null) =>
        new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
            .Select(d => new WeeklyHours(d, opens, closes, breakStarts, breakEnds));

    public static DateTimeOffset At(DateOnly day, int hour, int minute = 0) =>
        new(day.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero);
}
