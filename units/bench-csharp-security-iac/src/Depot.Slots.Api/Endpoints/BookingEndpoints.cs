using System.ComponentModel.DataAnnotations;
using Depot.Slots.Api.Security;
using Depot.Slots.Core.Bookings;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Depot.Slots.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api").WithTags("Bookings");

        group.MapGet("/docks/{dockCode}/bookings", ListDayAsync)
            .RequireAuthorization(AuthorizationPolicies.SlotsRead);
        group.MapPost("/bookings", CreateAsync)
            .RequireAuthorization(AuthorizationPolicies.SlotsWrite);
        group.MapDelete("/bookings/{id:guid}", CancelAsync)
            .RequireAuthorization(AuthorizationPolicies.SlotsWrite);
        return routes;
    }

    private static async Task<Results<Ok<BookingResponse[]>, ValidationProblem>> ListDayAsync(
        string dockCode, DateOnly date, BookingService bookings, CancellationToken cancellationToken)
    {
        if (dockCode.Length != 3)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(dockCode)] = ["A dock code is one letter followed by two digits."],
            });
        }

        var day = await bookings.ListDayAsync(dockCode, date, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(day.Select(BookingResponse.From).ToArray());
    }

    private static async Task<Results<Created<BookingResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CreateBookingRequest request, BookingService bookings, CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var outcome = await bookings.BookAsync(
            new BookingRequest(request.DockCode, request.CarrierReference, request.StartsAt.GetValueOrDefault(), request.DurationMinutes),
            cancellationToken).ConfigureAwait(false);

        if (outcome.Booking is { } booking)
        {
            return TypedResults.Created($"/api/docks/{booking.DockCode}/bookings", BookingResponse.From(booking));
        }

        var status = outcome.Failure == BookingFailure.Conflict ? StatusCodes.Status409Conflict : StatusCodes.Status422UnprocessableEntity;
        return TypedResults.Problem(outcome.Detail, statusCode: status);
    }

    private static async Task<Results<NoContent, NotFound>> CancelAsync(
        Guid id, BookingService bookings, CancellationToken cancellationToken) =>
        await bookings.CancelAsync(id, cancellationToken).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    private static Dictionary<string, string[]> Validate(CreateBookingRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results
            .SelectMany(r => r.MemberNames.Select(member => (member, message: r.ErrorMessage ?? "Invalid value.")))
            .GroupBy(e => e.member, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(e => e.message).ToArray(), StringComparer.Ordinal);
    }
}
