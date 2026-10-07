using Polly;
using Polly.Retry;
using Polly.Extensions.Http;

namespace Invoicing.Api.Branding;

/// <summary>Retries 5xx, 408 and network failures three times with exponential back-off.</summary>
public sealed class TransientRetryHandler : DelegatingHandler
{
    private static readonly AsyncRetryPolicy<HttpResponseMessage> Policy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Policy.ExecuteAsync(token => base.SendAsync(request, token), cancellationToken);
}
