using ClinicScheduling.Api.Contracts;
using ClinicScheduling.Api.Security;
using ClinicScheduling.Application;
using ClinicScheduling.Application.Availability;
using ClinicScheduling.Domain.Availability;

namespace ClinicScheduling.Api.Endpoints;

public static class AvailabilityEndpoints
{
    public static IEndpointRouteBuilder MapAvailabilityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/practitioners/{practitionerId:guid}/slots", FindAsync)
            .WithTags("Availability")
            .RequireAuthorization(AuthorizationPolicies.AppointmentsRead);
        return app;
    }

    private static async Task<IResult> FindAsync(
        Guid practitionerId,
        DateOnly from,
        DateOnly to,
        int durationMinutes,
        string? skill,
        bool? telehealth,
        FindSlotsHandler handler,
        CancellationToken cancellationToken)
    {
        var errors = new RequestErrors()
            .Require(to >= from && to.DayNumber - from.DayNumber <= 31, nameof(to), "Search at most 31 days at a time.")
            .Require(durationMinutes is >= 10 and <= 180, nameof(durationMinutes), "Appointments last 10 to 180 minutes.");
        if (errors.Any)
        {
            return Results.ValidationProblem(errors.ToDictionary());
        }

        var query = new SlotQuery(from, to, TimeSpan.FromMinutes(durationMinutes), skill, telehealth ?? false);
        var result = await handler.HandleAsync(practitionerId, query, cancellationToken).ConfigureAwait(false);
        return result is { Status: OperationStatus.Ok, Value: { } slots }
            ? Results.Ok(slots.Select(SlotResponse.From).ToList())
            : EndpointResults.Problem(result);
    }
}
