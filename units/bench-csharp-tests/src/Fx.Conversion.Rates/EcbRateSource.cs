using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Fx.Conversion.Rates.Ecb;

/// <summary>Fetches the ECB daily reference rates over HTTPS, with retries (<see cref="EcbResilience"/>).</summary>
public sealed partial class EcbRateSource(
    IHttpClientFactory httpClients,
    ResiliencePipeline pipeline,
    IOptions<EcbOptions> options,
    ILogger<EcbRateSource> logger) : IRateSource
{
    public const string DefaultFeed = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";

    /// <summary>The named <see cref="HttpClient"/> this source uses.</summary>
    public const string HttpClientName = "ecb";

    public async Task<RateTable> GetLatestAsync(CancellationToken cancellationToken)
    {
        var feed = options.Value.FeedUri;
        using var http = httpClients.CreateClient(HttpClientName);
        var xml = await pipeline.ExecuteAsync(
            static async (state, token) =>
            {
                using var response = await state.http.GetAsync(state.feed, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            },
            (http, feed),
            cancellationToken).ConfigureAwait(false);

        var table = EcbXmlParser.Parse(xml);
        LogFetched(feed, table.PublishedOn, table.Rates.Count);
        return table;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetched {Feed}: rates of {PublishedOn} ({Count} currencies)")]
    private partial void LogFetched(Uri feed, DateOnly publishedOn, int count);
}
