using ClinicScheduling.Infrastructure.Holidays;

namespace ClinicScheduling.UnitTests.Holidays;

public sealed class TableHolidayCalendarTests
{
    private readonly TableHolidayCalendar _calendar = new();

    [Theory]
    [InlineData("DK", 2026, 4, 2)]
    [InlineData("dk", 2026, 12, 24)]
    [InlineData("DE", 2026, 10, 3)]
    [InlineData("SE", 2026, 6, 19)]
    [InlineData("NO", 2027, 5, 17)]
    public void KnowsTheTabledHolidays(string country, int year, int month, int day)
    {
        Assert.True(_calendar.IsHoliday(country, new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData("DK", 2026, 3, 2)]
    [InlineData("DE", 2026, 6, 5)]
    [InlineData("FR", 2026, 1, 1)]
    public void TreatsEverythingElseAsAWorkingDay(string country, int year, int month, int day)
    {
        Assert.False(_calendar.IsHoliday(country, new DateOnly(year, month, day)));
    }
}
