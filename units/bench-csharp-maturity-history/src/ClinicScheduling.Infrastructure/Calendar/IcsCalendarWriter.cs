using System.Globalization;
using System.Text;

namespace ClinicScheduling.Infrastructure.Calendar;

/// <summary>One event of an iCalendar file.</summary>
public sealed record CalendarEvent(string Uid, DateTimeOffset Start, DateTimeOffset End, string Summary, string? Location, DateTimeOffset Stamp);

/// <summary>
/// Writes iCalendar (RFC 5545) text: CRLF line endings, TEXT values escaped (§3.3.11) and content lines folded at
/// 75 octets (§3.1) without splitting a UTF-8 sequence. Times are written in UTC.
/// </summary>
public static class IcsCalendarWriter
{
    private const int MaxOctetsPerLine = 75;
    private const string ProductId = "-//ClinicScheduling//Appointments//EN";

    public static string Write(IEnumerable<CalendarEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var ics = new StringBuilder();
        AppendLine(ics, "BEGIN:VCALENDAR");
        AppendLine(ics, "VERSION:2.0");
        AppendLine(ics, "PRODID:" + ProductId);
        AppendLine(ics, "CALSCALE:GREGORIAN");
        foreach (var e in events)
        {
            AppendLine(ics, "BEGIN:VEVENT");
            AppendLine(ics, "UID:" + Escape(e.Uid));
            AppendLine(ics, "DTSTAMP:" + Utc(e.Stamp));
            AppendLine(ics, "DTSTART:" + Utc(e.Start));
            AppendLine(ics, "DTEND:" + Utc(e.End));
            AppendLine(ics, "SUMMARY:" + Escape(e.Summary));
            if (!string.IsNullOrEmpty(e.Location))
            {
                AppendLine(ics, "LOCATION:" + Escape(e.Location));
            }

            AppendLine(ics, "END:VEVENT");
        }

        AppendLine(ics, "END:VCALENDAR");
        return ics.ToString();
    }

    /// <summary>Escapes a TEXT value: backslash, semicolon, comma and line breaks.</summary>
    public static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    /// <summary>Splits a content line into physical lines of at most 75 octets; continuations start with a space.</summary>
    public static IEnumerable<string> Fold(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        var current = new StringBuilder();
        var octets = 0;
        var elements = StringInfo.GetTextElementEnumerator(line);
        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();
            var size = Encoding.UTF8.GetByteCount(element);
            if (octets + size > MaxOctetsPerLine)
            {
                yield return current.ToString();
                current.Clear().Append(' ');
                octets = 1;
            }

            current.Append(element);
            octets += size;
        }

        yield return current.ToString();
    }

    private static void AppendLine(StringBuilder ics, string line)
    {
        foreach (var physical in Fold(line))
        {
            ics.Append(physical).Append("\r\n");
        }
    }

    private static string Utc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
}
