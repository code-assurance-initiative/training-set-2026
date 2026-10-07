using System.Globalization;
using ClinicScheduling.Domain.Appointments;
using Reason = ClinicScheduling.Domain.Appointments.CancellationReason;

namespace ClinicScheduling.Api.Contracts;

public sealed record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    Guid PractitionerId,
    Guid ClinicId,
    string StartsAt,
    string EndsAt,
    int DurationMinutes,
    string Status,
    bool IsTelehealth,
    Guid? SeriesId,
    string? CancellationReason,
    string? CancelledAt,
    int RescheduleCount)
{
    public static AppointmentResponse From(Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        return new AppointmentResponse(
            appointment.Id,
            appointment.PatientId,
            appointment.PractitionerId,
            appointment.ClinicId,
            Timestamp(appointment.Time.Start),
            Timestamp(appointment.Time.End),
            (int)appointment.Time.Duration.TotalMinutes,
            StatusCode(appointment.Status),
            appointment.IsTelehealth,
            appointment.SeriesId,
            appointment.CancellationReason is { } reason ? ReasonCode(reason) : null,
            appointment.CancelledAt is { } at ? Timestamp(at) : null,
            appointment.RescheduleCount);
    }

    private static string Timestamp(DateTimeOffset instant) =>
        instant.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

    private static string StatusCode(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Booked => "booked",
        AppointmentStatus.Cancelled => "cancelled",
        AppointmentStatus.Completed => "completed",
        AppointmentStatus.NoShow => "no-show",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static string ReasonCode(Reason reason) => reason switch
    {
        Reason.PatientRequest => "patient-request",
        Reason.PatientIllness => "patient-illness",
        Reason.PractitionerUnavailable => "practitioner-unavailable",
        Reason.ClinicClosed => "clinic-closed",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };
}
