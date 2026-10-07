using ClinicScheduling.Api.Contracts;
using ClinicScheduling.Api.Security;
using ClinicScheduling.Application;
using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Application.Booking;

namespace ClinicScheduling.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/appointments").WithTags("Appointments");
        group.MapPost("/", BookAsync).RequireAuthorization(AuthorizationPolicies.AppointmentsWrite);
        group.MapGet("/{id:guid}", GetAsync).RequireAuthorization(AuthorizationPolicies.AppointmentsRead).WithName("GetAppointment");
        group.MapPost("/{id:guid}/cancellation", CancelAsync).RequireAuthorization(AuthorizationPolicies.AppointmentsWrite);
        group.MapPost("/{id:guid}/reschedule", RescheduleAsync).RequireAuthorization(AuthorizationPolicies.AppointmentsWrite);
        group.MapPost("/{id:guid}/no-show", NoShowAsync).RequireAuthorization(AuthorizationPolicies.AppointmentsWrite);
        return app;
    }

    private static async Task<IResult> BookAsync(BookAppointmentRequest request, BookAppointmentHandler handler, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Any)
        {
            return Results.ValidationProblem(errors.ToDictionary());
        }

        var command = new BookAppointmentCommand(
            request.PatientId, request.PractitionerId, request.ClinicId, request.Start, TimeSpan.FromMinutes(request.DurationMinutes), request.Telehealth);
        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return result is { Status: OperationStatus.Ok, Value: { } appointment }
            ? Results.CreatedAtRoute("GetAppointment", new { id = appointment.Id }, AppointmentResponse.From(appointment))
            : EndpointResults.Problem(result);
    }

    private static async Task<IResult> GetAsync(Guid id, IAppointmentRepository appointments, CancellationToken cancellationToken)
    {
        var appointment = await appointments.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return appointment is null ? Results.NotFound() : Results.Ok(AppointmentResponse.From(appointment));
    }

    private static async Task<IResult> CancelAsync(Guid id, CancelAppointmentRequest request, CancelAppointmentHandler handler, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Any || request.ParsedReason is not { } reason)
        {
            return Results.ValidationProblem(errors.ToDictionary());
        }

        var result = await handler.HandleAsync(new CancelAppointmentCommand(id, reason), cancellationToken).ConfigureAwait(false);
        return result is { Status: OperationStatus.Ok, Value: { } decision }
            ? Results.Ok(CancellationResponse.From(decision))
            : EndpointResults.Problem(result);
    }

    private static async Task<IResult> RescheduleAsync(Guid id, RescheduleAppointmentRequest request, RescheduleAppointmentHandler handler, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Any)
        {
            return Results.ValidationProblem(errors.ToDictionary());
        }

        var result = await handler.HandleAsync(new RescheduleAppointmentCommand(id, request.NewStart), cancellationToken).ConfigureAwait(false);
        return result is { Status: OperationStatus.Ok, Value: { } appointment }
            ? Results.Ok(AppointmentResponse.From(appointment))
            : EndpointResults.Problem(result);
    }

    private static async Task<IResult> NoShowAsync(Guid id, RecordNoShowHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken).ConfigureAwait(false);
        return result is { Status: OperationStatus.Ok, Value: { } decision }
            ? Results.Ok(CancellationResponse.From(decision))
            : EndpointResults.Problem(result);
    }
}
