using ClinicScheduling.Api.Contracts;
using ClinicScheduling.Api.Security;
using ClinicScheduling.Application;
using ClinicScheduling.Application.Series;

namespace ClinicScheduling.Api.Endpoints;

public static class SeriesEndpoints
{
    public static IEndpointRouteBuilder MapSeriesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/series", BookAsync)
            .WithTags("Series")
            .RequireAuthorization(AuthorizationPolicies.AppointmentsWrite);
        return app;
    }

    private static async Task<IResult> BookAsync(BookSeriesRequest request, BookTreatmentSeriesHandler handler, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Any || request.ParsedRule is not { } rule)
        {
            return Results.ValidationProblem(errors.ToDictionary());
        }

        var command = new BookTreatmentSeriesCommand(
            request.PatientId, request.PractitionerId, request.ClinicId, request.FirstDay, request.StartTime,
            TimeSpan.FromMinutes(request.DurationMinutes), rule);
        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return result is { Status: OperationStatus.Ok, Value: { } sessions }
            ? Results.Ok(sessions.Select(AppointmentResponse.From).ToList())
            : EndpointResults.Problem(result);
    }
}
