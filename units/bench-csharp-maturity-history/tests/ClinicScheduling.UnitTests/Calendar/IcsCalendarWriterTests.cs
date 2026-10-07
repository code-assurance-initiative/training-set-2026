using System.Text;
using ClinicScheduling.Infrastructure.Calendar;

namespace ClinicScheduling.UnitTests.Calendar;

public sealed class IcsCalendarWriterTests
{
    private static readonly DateTimeOffset Start = new(2026, 3, 5, 9, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void WritesACalendarWithCrlfLineEndings()
    {
        var ics = IcsCalendarWriter.Write([new CalendarEvent("a1@test", Start, Start.AddHours(1), "Physiotherapy", "Room 2", Start.AddDays(-1))]);

        Assert.StartsWith("BEGIN:VCALENDAR\r\nVERSION:2.0\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("\r\nDTSTART:20260305T080000Z\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("\r\nDTEND:20260305T090000Z\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("\r\nLOCATION:Room 2\r\n", ics, StringComparison.Ordinal);
        Assert.EndsWith("END:VEVENT\r\nEND:VCALENDAR\r\n", ics, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", ics.Replace("\r\n", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public void OmitsAnEmptyLocation()
    {
        var ics = IcsCalendarWriter.Write([new CalendarEvent("a1@test", Start, Start.AddHours(1), "Video", null, Start)]);

        Assert.DoesNotContain("LOCATION", ics, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("a,b", "a\\,b")]
    [InlineData("a;b", "a\\;b")]
    [InlineData("a\\b", "a\\\\b")]
    [InlineData("line1\nline2", "line1\\nline2")]
    [InlineData("line1\r\nline2", "line1\\nline2")]
    public void EscapesTextValues(string value, string expected)
    {
        Assert.Equal(expected, IcsCalendarWriter.Escape(value));
    }

    [Fact]
    public void FoldsLongLinesAtSeventyFiveOctets()
    {
        var lines = IcsCalendarWriter.Fold("SUMMARY:" + new string('x', 200)).ToList();

        Assert.Equal(3, lines.Count);
        Assert.All(lines, l => Assert.True(Encoding.UTF8.GetByteCount(l) <= 75));
        Assert.All(lines.Skip(1), l => Assert.StartsWith(" ", l, StringComparison.Ordinal));
        Assert.Equal("SUMMARY:" + new string('x', 200), string.Concat(lines.Select((l, i) => i == 0 ? l : l[1..])));
    }

    [Fact]
    public void NeverSplitsAMultiByteCharacter()
    {
        var line = "SUMMARY:" + string.Concat(Enumerable.Repeat("æøå", 30));

        var lines = IcsCalendarWriter.Fold(line).ToList();

        Assert.All(lines, l => Assert.True(Encoding.UTF8.GetByteCount(l) <= 75));
        Assert.Equal(line, string.Concat(lines.Select((l, i) => i == 0 ? l : l[1..])));
    }

    [Fact]
    public void LeavesShortLinesAlone()
    {
        Assert.Equal(["VERSION:2.0"], IcsCalendarWriter.Fold("VERSION:2.0"));
    }
}
