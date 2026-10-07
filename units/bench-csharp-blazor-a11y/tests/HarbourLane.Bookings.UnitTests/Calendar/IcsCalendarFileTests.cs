using HarbourLane.Bookings.Bookings;
using HarbourLane.Bookings.Calendar;
using HarbourLane.Bookings.UnitTests.TestSupport;

namespace HarbourLane.Bookings.UnitTests.Calendar;

public sealed class IcsCalendarFileTests
{
    private static readonly Booking Booking = new()
    {
        Reference = "HL-7K2Q9M",
        RoomId = Requests.Hall.Id,
        Start = new DateTimeOffset(2026, 10, 6, 10, 0, 0, TimeSpan.FromHours(1)),
        End = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.FromHours(1)),
        OrganiserName = "Priya Natarajan",
        OrganiserEmail = "priya@harbourlane-choir.org",
        Attendees = 30,
    };

    [Fact]
    public void The_event_is_written_in_utc_with_crlf_line_endings()
    {
        var ics = IcsCalendarFile.Write(Booking, Requests.Hall, new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.Zero));

        Assert.Contains("DTSTART:20261006T090000Z\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("DTEND:20261006T110000Z\r\n", ics, StringComparison.Ordinal);
        Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics, StringComparison.Ordinal);
        Assert.EndsWith("END:VCALENDAR\r\n", ics, StringComparison.Ordinal);
    }

    [Fact]
    public void Commas_in_text_values_are_escaped()
    {
        var ics = IcsCalendarFile.Write(Booking, Requests.Hall, DateTimeOffset.UnixEpoch);

        Assert.Contains("LOCATION:Harbour Lane Community Centre\\, Main hall\\, Ground floor\r\n", ics, StringComparison.Ordinal);
    }

    [Fact]
    public void The_file_is_named_after_the_reference()
    {
        Assert.Equal("HL-7K2Q9M.ics", IcsCalendarFile.FileName(Booking));
    }
}
