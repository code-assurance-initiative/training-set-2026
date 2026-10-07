using System.Globalization;
using System.Text;
using HarbourLane.Bookings.Bookings;
using HarbourLane.Bookings.Rooms;

namespace HarbourLane.Bookings.Calendar;

/// <summary>Writes a booking as an iCalendar (RFC 5545) file a visitor can add to their own calendar.</summary>
public static class IcsCalendarFile
{
    public static string FileName(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);
        return $"{booking.Reference}.ics";
    }

    public static string Write(Booking booking, Room room, DateTimeOffset stamp)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(room);
        var ics = new StringBuilder();
        Line(ics, "BEGIN:VCALENDAR");
        Line(ics, "VERSION:2.0");
        Line(ics, "PRODID:-//Harbour Lane Community Centre//Room booking//EN");
        Line(ics, "BEGIN:VEVENT");
        Line(ics, $"UID:{booking.Reference}@bookings.harbourlane.org");
        Line(ics, $"DTSTAMP:{Utc(stamp)}");
        Line(ics, $"DTSTART:{Utc(booking.Start)}");
        Line(ics, $"DTEND:{Utc(booking.End)}");
        Line(ics, $"SUMMARY:{Escape(room.Name)} booking {booking.Reference}");
        Line(ics, $"LOCATION:{Escape($"Harbour Lane Community Centre, {room.Name}, {room.Floor}")}");
        Line(ics, "END:VEVENT");
        Line(ics, "END:VCALENDAR");
        return ics.ToString();
    }

    private static void Line(StringBuilder ics, string text) => ics.Append(text).Append("\r\n");

    private static string Utc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal);
}
