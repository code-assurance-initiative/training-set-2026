using System.Globalization;

namespace ClinicScheduling.Domain.Recurrence;

/// <summary>
/// How a treatment series repeats: every <see cref="IntervalWeeks"/> weeks on the given weekdays, for a number of
/// sessions. Written in a compact text form for the API, e.g. <c>WEEKLY;INTERVAL=1;COUNT=8;BYDAY=MO,TH</c>, a subset
/// of the iCalendar RRULE grammar.
/// </summary>
public sealed record RecurrenceRule
{
    public const int MaxSessions = 52;

    private static readonly Dictionary<string, DayOfWeek> DayCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MO"] = DayOfWeek.Monday,
        ["TU"] = DayOfWeek.Tuesday,
        ["WE"] = DayOfWeek.Wednesday,
        ["TH"] = DayOfWeek.Thursday,
        ["FR"] = DayOfWeek.Friday,
        ["SA"] = DayOfWeek.Saturday,
        ["SU"] = DayOfWeek.Sunday,
    };

    public RecurrenceRule(int intervalWeeks, int count, IReadOnlyCollection<DayOfWeek> days)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(intervalWeeks, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxSessions);
        ArgumentNullException.ThrowIfNull(days);
        if (days.Count == 0)
        {
            throw new ArgumentException("A recurrence needs at least one weekday.", nameof(days));
        }

        IntervalWeeks = intervalWeeks;
        Count = count;
        Days = [.. days.Distinct().OrderBy(d => ((int)d + 6) % 7)];
    }

    public int IntervalWeeks { get; }

    public int Count { get; }

    /// <summary>The weekdays, Monday first.</summary>
    public IReadOnlyList<DayOfWeek> Days { get; }

    public static bool TryParse(string? text, out RecurrenceRule? rule)
    {
        rule = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || !parts[0].Equals("WEEKLY", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var interval = 1;
        var count = 0;
        var days = new List<DayOfWeek>();
        foreach (var part in parts.Skip(1))
        {
            var pair = part.Split('=', 2);
            var ok = pair.Length == 2 && pair[0].ToUpperInvariant() switch
            {
                "INTERVAL" => int.TryParse(pair[1], NumberStyles.None, CultureInfo.InvariantCulture, out interval),
                "COUNT" => int.TryParse(pair[1], NumberStyles.None, CultureInfo.InvariantCulture, out count),
                "BYDAY" => TryParseDays(pair[1], days),
                _ => false,
            };
            if (!ok)
            {
                return false;
            }
        }

        if (!IsValid(interval, count, days))
        {
            return false;
        }

        rule = new RecurrenceRule(interval, count, days);
        return true;
    }

    private static bool IsValid(int interval, int count, List<DayOfWeek> days) =>
        interval >= 1 && count is >= 1 and <= MaxSessions && days.Count > 0;

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"WEEKLY;INTERVAL={IntervalWeeks};COUNT={Count};BYDAY={string.Join(',', Days.Select(Code))}");

    private static bool TryParseDays(string text, List<DayOfWeek> days)
    {
        foreach (var code in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!DayCodes.TryGetValue(code, out var day))
            {
                return false;
            }

            days.Add(day);
        }

        return days.Count > 0;
    }

    private static string Code(DayOfWeek day) => DayCodes.First(kv => kv.Value == day).Key;
}
