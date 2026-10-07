using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ParcelTracking.Infrastructure.Health;

/// <summary>Readiness check: can the carrier API be reached?</summary>
public sealed class CarrierApiHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public const string HttpClientName = "carrier-api-health";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        try
        {
            using var response = await client.GetAsync("v2/ping", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Degraded($"Carrier API answered HTTP {(int)response.StatusCode}.");
            }
        }
        catch (Exception)
        {
        }

        return HealthCheckResult.Healthy();
    }
}
