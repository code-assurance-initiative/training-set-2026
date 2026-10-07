namespace ClinicScheduling.Api.Contracts;

public sealed record RescheduleAppointmentRequest(DateTimeOffset NewStart)
{
    public RequestErrors Validate() =>
        new RequestErrors().Require(NewStart != default, nameof(NewStart), "A new start time is required.");
}
