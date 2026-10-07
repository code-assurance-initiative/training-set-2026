using ClinicScheduling.Domain.Appointments;

namespace ClinicScheduling.Api.Contracts;

public sealed record CancelAppointmentRequest(string? Reason)
{
    private static readonly Dictionary<string, CancellationReason> Reasons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["patient-request"] = CancellationReason.PatientRequest,
        ["patient-illness"] = CancellationReason.PatientIllness,
        ["practitioner-unavailable"] = CancellationReason.PractitionerUnavailable,
        ["clinic-closed"] = CancellationReason.ClinicClosed,
    };

    public CancellationReason? ParsedReason => Reason is not null && Reasons.TryGetValue(Reason, out var reason) ? reason : null;

    public RequestErrors Validate() =>
        new RequestErrors().Require(ParsedReason is not null, nameof(Reason), "Reason must be one of: " + string.Join(", ", Reasons.Keys) + ".");
}
