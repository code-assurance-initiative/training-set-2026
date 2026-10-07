using System.Globalization;
using System.Text;
using System.Text.Json;
using ClinicScheduling.Domain.Appointments;

namespace ClinicScheduling.Infrastructure.PatientPortal;

/// <summary>
/// Writes the appointment feed the patient portal polls: one JSON document per patient, newest appointment first.
/// The portal renders it as it is, so it carries the fields the portal shows.
/// </summary>
public static class PortalAppointmentFeed
{
    public static string Serialize(Guid patientId, IEnumerable<Appointment> appointments, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(appointments);
        ArgumentNullException.ThrowIfNull(zone);

        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            json.WriteStartObject();
            json.WriteString("patientId", patientId);
            json.WriteStartArray("appointments");
            foreach (var a in appointments.OrderByDescending(a => a.Time.Start))
            {
                json.WriteStartObject();
                json.WriteString("id", a.Id);
                json.WriteString("practitionerId", a.PractitionerId);
                json.WriteString("clinicId", a.ClinicId);
                json.WriteString("startsAt", Local(a.Time.Start, zone));
                json.WriteString("endsAt", Local(a.Time.End, zone));
                json.WriteNumber("durationMinutes", (int)a.Time.Duration.TotalMinutes);
                json.WriteString("status", Status(a.Status));
                json.WriteBoolean("isTelehealth", a.IsTelehealth);
                WriteNullable(json, "seriesId", a.SeriesId?.ToString());
                WriteNullable(json, "cancellationReason", a.CancellationReason is { } reason ? Reason(reason) : null);
                WriteNullable(json, "cancelledAt", a.CancelledAt is { } at ? Local(at, zone) : null);
                json.WriteNumber("rescheduleCount", a.RescheduleCount);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static string Local(DateTimeOffset instant, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(instant, zone).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

    private static string Status(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Booked => "booked",
        AppointmentStatus.Cancelled => "cancelled",
        AppointmentStatus.Completed => "completed",
        AppointmentStatus.NoShow => "no-show",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static string Reason(CancellationReason reason) => reason switch
    {
        CancellationReason.PatientRequest => "patient-request",
        CancellationReason.PatientIllness => "patient-illness",
        CancellationReason.PractitionerUnavailable => "practitioner-unavailable",
        CancellationReason.ClinicClosed => "clinic-closed",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };

    private static void WriteNullable(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null)
        {
            json.WriteNull(name);
        }
        else
        {
            json.WriteString(name, value);
        }
    }
}
