using Shipping.Rates.Api.Contracts;
using Shipping.Rates.Api.Security;
using Shipping.Rates.Core.Carriers;
using Shipping.Rates.Core.Pricing;

namespace Shipping.Rates.Api.Endpoints;

public static class QuoteEndpoints
{
    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/quotes", QuoteAsync).RequireAuthorization(AuthorizationPolicies.RatesRead);
        return routes;
    }

    private static async Task<IResult> QuoteAsync(
        QuoteRequestDto body,
        QuoteService quotes,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (!QuoteRequestMapper.TryMap(body, today, out var request, out var errors) || request is null)
        {
            return Results.ValidationProblem(errors);
        }

        try
        {
            var result = await quotes.QuoteAsync(request, cancellationToken);
            return Results.Ok(result.Select(q => new QuoteResponseDto(
                q.Carrier, CarrierRegistry.DisplayName(q.Carrier), q.Level, q.Total.Amount, q.Total.Currency, q.TransitDays)));
        }
        catch (RateCardUnavailableException ex)
        {
            return Results.Problem($"Rates for {ex.Carrier} are temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (ShipmentRejectedException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }
}
