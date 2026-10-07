using ClinicScheduling.Domain.Availability;
using ClinicScheduling.Domain.Holidays;

namespace ClinicScheduling.Domain.Recurrence;

/// <summary>
/// Expands a <see cref="RecurrenceRule"/> into the dated sessions of a treatment series. A session that would fall on
/// a public holiday moves to the next working day of the same week if there is one that is not already a session
/// day; otherwise it is dropped and the series is extended by one occurrence so that the patient still gets
/// <see cref="RecurrenceRule.Count"/> sessions.
/// </summary>
public sealed class RecurrenceExpander(IHolidayCalendar holidays)
{
    /// <summary>Upper bound on how far a series may run, so a rule that can never be satisfied terminates.</summary>
    public const int MaxWeeks = 104;

    public IReadOnlyList<TimeRange> Expand(
        RecurrenceRule rule,
        DateOnly firstDay,
        TimeOnly startTime,
        TimeSpan duration,
        TimeZoneInfo zone,
        string countryCode)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);

        var sessions = new List<TimeRange>(rule.Count);
        var weekStart = firstDay.AddDays(-(((int)firstDay.DayOfWeek + 6) % 7));
        for (var week = 0; sessions.Count < rule.Count && week < MaxWeeks; week += rule.IntervalWeeks)
        {
            var monday = weekStart.AddDays(7 * week);
            var taken = new HashSet<DateOnly>();
            foreach (var day in rule.Days)
            {
                var date = monday.AddDays(((int)day + 6) % 7);
                if (date < firstDay || sessions.Count == rule.Count)
                {
                    continue;
                }

                var actual = holidays.IsHoliday(countryCode, date) ? NextFreeDay(date, monday, rule, taken, countryCode) : date;
                if (actual is { } d)
                {
                    taken.Add(d);
                    sessions.Add(Session(d, startTime, duration, zone));
                }
            }
        }

        return sessions;
    }

    private DateOnly? NextFreeDay(DateOnly date, DateOnly monday, RecurrenceRule rule, HashSet<DateOnly> taken, string countryCode)
    {
        var friday = monday.AddDays(4);
        for (var candidate = date.AddDays(1); candidate <= friday; candidate = candidate.AddDays(1))
        {
            var isSessionDay = rule.Days.Contains(candidate.DayOfWeek);
            if (!isSessionDay && !taken.Contains(candidate) && !holidays.IsHoliday(countryCode, candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static TimeRange Session(DateOnly day, TimeOnly startTime, TimeSpan duration, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(startTime, DateTimeKind.Unspecified);
        var start = new DateTimeOffset(local, zone.GetUtcOffset(local));
        return new TimeRange(start, start + duration);
    }
}
