namespace ClinicScheduling.Api.Contracts;

public sealed record BookAppointmentRequest(
    Guid PatientId, Guid PractitionerId, Guid ClinicId, DateTimeOffset Start, int DurationMinutes, bool Telehealth)
{
    public RequestErrors Validate() =>
        new RequestErrors()
            .Require(PatientId != Guid.Empty, nameof(PatientId), "A patient is required.")
            .Require(PractitionerId != Guid.Empty, nameof(PractitionerId), "A practitioner is required.")
            .Require(ClinicId != Guid.Empty, nameof(ClinicId), "A clinic is required.")
            .Require(Start != default, nameof(Start), "A start time is required.")
            .Require(DurationMinutes is >= 10 and <= 180, nameof(DurationMinutes), "Appointments last 10 to 180 minutes.");
}
