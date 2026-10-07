using ClinicScheduling.Api.Contracts;
using ClinicScheduling.Api.Security;
using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Domain.Availability;

namespace ClinicScheduling.Api.Endpoints;

public static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/practitioners/{practitionerId:guid}/schedule", UpsertAsync)
            .WithTags("Schedules")
            .RequireAuthorization(AuthorizationPolicies.SchedulesWrite);
        return app;
    }

    private static async Task<IResult> UpsertAsync(
        Guid practitionerId, ScheduleRequest request, IScheduleRepository schedules, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Any || request.Zone is not { } zone || request.CountryCode is not { } country || request.Hours is not { } hours)
        {
            return Results.ValidationProblem(errors.ToDictionary());
        }

        var schedule = new PractitionerSchedule(
            practitionerId,
            country,
            zone,
            hours.Select(h => new WeeklyHours(h.Day, h.Opens, h.Closes, h.BreakStarts, h.BreakEnds, h.InClinicOnly)),
            request.Skills ?? [],
            request.OffersTelehealth,
            request.MaxAppointmentsPerDay);
        await schedules.UpsertAsync(schedule, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
