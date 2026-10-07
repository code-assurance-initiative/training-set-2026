using System.Collections.Frozen;
using ClinicScheduling.Domain.Holidays;

namespace ClinicScheduling.Infrastructure.Holidays;

internal readonly record struct PublicHoliday(DateOnly Date, string CountryCode, string Name);

/// <summary>The holiday calendar backed by the generated <see cref="PublicHolidays"/> table.</summary>
public sealed class TableHolidayCalendar : IHolidayCalendar
{
    private static readonly FrozenSet<(string CountryCode, DateOnly Date)> Days =
        PublicHolidays.All.Select(h => (h.CountryCode, h.Date)).ToFrozenSet();

    public bool IsHoliday(string countryCode, DateOnly day)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);
        return Days.Contains((countryCode.ToUpperInvariant(), day));
    }
}
