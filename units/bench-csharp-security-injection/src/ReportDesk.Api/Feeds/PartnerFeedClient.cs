using Microsoft.Extensions.Options;

namespace ReportDesk.Api.Feeds;

/// <summary>Pulls a partner's records feed. Only configured partner hosts are reachable (ADR 0003).</summary>
public sealed class PartnerFeedClient(HttpClient client, IOptions<FeedOptions> options)
{
    private readonly HashSet<string> _allowedHosts = new(options.Value.AllowedHosts, StringComparer.OrdinalIgnoreCase);

    public async Task<string> FetchAsync(string feedUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var feedUri)
            || feedUri.Scheme != Uri.UriSchemeHttps
            || !_allowedHosts.Contains(feedUri.Host))
        {
            throw new FeedNotAllowedException($"'{feedUrl}' is not a configured partner feed.");
        }

        using var response = await client.GetAsync(feedUri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
