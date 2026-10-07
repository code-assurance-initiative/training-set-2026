namespace ClinicScheduling.Domain.Appointments;

public enum CancellationReason
{
    PatientRequest,
    PatientIllness,
    PractitionerUnavailable,
    ClinicClosed,
}
