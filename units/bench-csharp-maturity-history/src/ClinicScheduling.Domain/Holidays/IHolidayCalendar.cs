namespace ClinicScheduling.Domain.Holidays;

/// <summary>Public holidays per country (ISO 3166-1 alpha-2), on which no clinic is open.</summary>
public interface IHolidayCalendar
{
    bool IsHoliday(string countryCode, DateOnly day);
}
