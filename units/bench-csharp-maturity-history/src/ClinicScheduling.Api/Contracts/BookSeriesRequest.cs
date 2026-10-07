using ClinicScheduling.Domain.Recurrence;

namespace ClinicScheduling.Api.Contracts;

public sealed record BookSeriesRequest(
    Guid PatientId, Guid PractitionerId, Guid ClinicId, DateOnly FirstDay, TimeOnly StartTime, int DurationMinutes, string? Recurrence)
{
    public RecurrenceRule? ParsedRule => RecurrenceRule.TryParse(Recurrence, out var rule) ? rule : null;

    public RequestErrors Validate() =>
        new RequestErrors()
            .Require(PatientId != Guid.Empty, nameof(PatientId), "A patient is required.")
            .Require(PractitionerId != Guid.Empty, nameof(PractitionerId), "A practitioner is required.")
            .Require(ClinicId != Guid.Empty, nameof(ClinicId), "A clinic is required.")
            .Require(DurationMinutes is >= 10 and <= 180, nameof(DurationMinutes), "Sessions last 10 to 180 minutes.")
            .Require(ParsedRule is not null, nameof(Recurrence), "Recurrence must look like WEEKLY;INTERVAL=1;COUNT=8;BYDAY=MO,TH.");
}
