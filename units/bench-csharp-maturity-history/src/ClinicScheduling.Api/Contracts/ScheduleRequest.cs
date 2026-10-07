namespace ClinicScheduling.Api.Contracts;

public sealed record WeeklyHoursRequest(
    DayOfWeek Day, TimeOnly Opens, TimeOnly Closes, TimeOnly? BreakStarts, TimeOnly? BreakEnds, bool InClinicOnly);

public sealed record ScheduleRequest(
    string? CountryCode,
    string? TimeZone,
    IReadOnlyList<WeeklyHoursRequest>? Hours,
    IReadOnlyList<string>? Skills,
    bool OffersTelehealth,
    int MaxAppointmentsPerDay)
{
    public TimeZoneInfo? Zone => TimeZone is not null && TimeZoneInfo.TryFindSystemTimeZoneById(TimeZone, out var zone) ? zone : null;

    public RequestErrors Validate() =>
        new RequestErrors()
            .Require(CountryCode is { Length: 2 }, nameof(CountryCode), "A two-letter country code is required.")
            .Require(Zone is not null, nameof(TimeZone), "An IANA time zone id is required.")
            .Require(Hours is { Count: > 0 } && Hours.All(h => h.Opens < h.Closes), nameof(Hours), "Opening hours must open before they close.")
            .Require(MaxAppointmentsPerDay is >= 1 and <= 40, nameof(MaxAppointmentsPerDay), "Between 1 and 40 appointments a day.");
}
