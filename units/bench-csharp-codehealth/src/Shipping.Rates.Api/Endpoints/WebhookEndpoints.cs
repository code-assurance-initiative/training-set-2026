using Shipping.Rates.Core.Accounts;

namespace Shipping.Rates.Api.Endpoints;

public static class WebhookEndpoints
{
    public const string SignatureHeader = "X-Carrier-Signature";

    /// <summary>
    /// Carriers post tracking events here. The endpoint is anonymous because carriers cannot obtain our access
    /// tokens; every request is authenticated by its HMAC signature instead.
    /// </summary>
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/webhooks/tracking", ReceiveAsync).AllowAnonymous();
        return routes;
    }

    private static async Task<IResult> ReceiveAsync(
        HttpRequest request,
        CarrierAccountManager accounts,
        TimeProvider clock,
        ILoggerFactory loggers,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        if (!accounts.VerifyWebhook(payload, request.Headers[SignatureHeader].ToString()))
        {
            return Results.Unauthorized();
        }

        accounts.RecordAudit($"tracking webhook, {payload.Length} bytes", clock.GetUtcNow());
        loggers.CreateLogger("Webhooks").LogInformation("Accepted a tracking webhook of {Length} bytes", payload.Length);
        return Results.Accepted();
    }
}
