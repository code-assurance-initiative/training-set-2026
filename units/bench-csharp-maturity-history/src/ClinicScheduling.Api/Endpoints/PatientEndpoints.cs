using ClinicScheduling.Api.Security;
using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Infrastructure.Calendar;
using ClinicScheduling.Infrastructure.PatientPortal;

namespace ClinicScheduling.Api.Endpoints;

/// <summary>What the patient portal reads: the appointment feed and a calendar subscription.</summary>
public static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/patients/{patientId:guid}")
            .WithTags("Patient portal")
            .RequireAuthorization(AuthorizationPolicies.PortalRead);
        group.MapGet("/feed", FeedAsync);
        group.MapGet("/calendar.ics", CalendarAsync);
        return app;
    }

    private static async Task<IResult> FeedAsync(
        Guid patientId, string? timeZone, IAppointmentRepository appointments, CancellationToken cancellationToken)
    {
        var zone = timeZone is not null && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var found) ? found : TimeZoneInfo.Utc;
        var mine = await appointments.ForPatientAsync(patientId, cancellationToken).ConfigureAwait(false);
        return Results.Text(PortalAppointmentFeed.Serialize(patientId, mine, zone), "application/json");
    }

    private static async Task<IResult> CalendarAsync(
        Guid patientId, IAppointmentRepository appointments, TimeProvider time, CancellationToken cancellationToken)
    {
        var mine = await appointments.ForPatientAsync(patientId, cancellationToken).ConfigureAwait(false);
        var stamp = time.GetUtcNow();
        var events = mine
            .Where(a => a.Status == AppointmentStatus.Booked)
            .Select(a => new CalendarEvent(
                $"{a.Id}@clinic-scheduling",
                a.Time.Start,
                a.Time.End,
                a.IsTelehealth ? "Video physiotherapy appointment" : "Physiotherapy appointment",
                a.IsTelehealth ? null : "Clinic",
                stamp));
        return Results.Text(IcsCalendarWriter.Write(events), "text/calendar");
    }
}
