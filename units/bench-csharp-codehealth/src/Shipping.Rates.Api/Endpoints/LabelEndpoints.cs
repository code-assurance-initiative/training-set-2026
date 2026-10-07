using System.Text.Json;
using Shipping.Rates.Api.Security;
using Shipping.Rates.Core.Carriers;
using Shipping.Rates.Core.Labels;

namespace Shipping.Rates.Api.Endpoints;

public static class LabelEndpoints
{
    public static IEndpointRouteBuilder MapLabelEndpoints(this IEndpointRouteBuilder routes)
    {
        var labels = routes.MapGroup("/api/labels").RequireAuthorization(AuthorizationPolicies.LabelsWrite);
        labels.MapPost("/", CreateAsync);
        labels.MapGet("/{id}", (string id, LabelArchive archive) =>
        {
            var zpl = archive.ReadAsync(id, CancellationToken.None).Result;
            return zpl is null ? Results.NotFound() : Results.Text(zpl, "application/zpl");
        });
        labels.MapPost("/{id}/void", VoidAsync);
        labels.MapPost("/{id}/email", ResendAsync);
        return routes;
    }

    private static async Task<IResult> CreateAsync(JsonElement body, LabelService service, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CreateLabelAsync(body, cancellationToken);
            return Results.Created($"/api/labels/{result.LabelId}", result);
        }
        catch (LabelValidationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [.. ex.Errors] });
        }
        catch (CarrierException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> VoidAsync(string id, LabelService service, CancellationToken cancellationToken) =>
        await service.VoidLabelAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> ResendAsync(string id, LabelService service, CancellationToken cancellationToken) =>
        await service.ResendEmailAsync(id, cancellationToken) ? Results.Accepted() : Results.NotFound();
}
