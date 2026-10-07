using System.Net;

namespace Invoicing.Api.Branding;

/// <summary>Fetches tenant logos from the branding service. Transient failures are retried by <see cref="TransientRetryHandler"/>.</summary>
public sealed partial class BrandingServiceClient(HttpClient http, ILogger<BrandingServiceClient> logger) : ILogoSource
{
    private const int MaxLogoBytes = 512 * 1024;

    public async Task<byte[]?> GetLogoAsync(string tenant, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenant);

        using var response = await http
            .GetAsync(new Uri($"tenants/{Uri.EscapeDataString(tenant)}/logo", UriKind.Relative), HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            LogNoLogo(tenant);
            return null;
        }

        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaxLogoBytes)
        {
            LogLogoTooLarge(tenant, response.Content.Headers.ContentLength.Value);
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant {Tenant} has no logo")]
    private partial void LogNoLogo(string tenant);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Logo of tenant {Tenant} is {Bytes} bytes; rendering without it")]
    private partial void LogLogoTooLarge(string tenant, long bytes);
}
