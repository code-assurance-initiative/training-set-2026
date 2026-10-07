using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Shipping.Rates.Core.Pricing;

/// <summary>Where a carrier's rate card is published.</summary>
public sealed record RateCardSource(string Carrier, Uri Uri);

/// <summary>
/// Keeps the carriers' current rate cards in memory, downloaded at start-up and refreshed in the background.
/// </summary>
public sealed class RateCardCache : IRateCardProvider, IDisposable
{
    private const string Component = "rate-card-cache";

    private readonly TimeSpan _ttl;
    private readonly HttpClient _http;
    private readonly IReadOnlyList<RateCardSource> _sources;
    private readonly TimeProvider _clock;
    private readonly ILogger<RateCardCache> _logger;
    private readonly Dictionary<string, RateCard> _cards = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();
    private readonly SemaphoreSlim _downloads = new(1, 1);
    private readonly Timer _refreshTimer;

    public RateCardCache(HttpClient http, IEnumerable<RateCardSource> sources, TimeProvider clock, ILogger<RateCardCache> logger, TimeSpan? ttl = null)
    {
        _http = http;
        _sources = [.. sources];
        _clock = clock;
        _logger = logger;
        _ttl = ttl ?? TimeSpan.FromMinutes(30);
        _refreshTimer = new Timer(_ => WarmUp(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public RateCard GetCard(string carrier)
    {
        lock (_gate)
        {
            if (_cards.TryGetValue(carrier, out var card) && !IsStale(card))
            {
                return card;
            }
        }

        throw new RateCardUnavailableException(carrier);
    }

    public async void WarmUp()
    {
        foreach (var source in _sources)
        {
            var card = await DownloadAsync(source.Uri);
            lock (_gate)
            {
                _cards[source.Carrier] = card;
            }
        }

        _logger.LogInformation($"{Component} warm-up finished");
    }

    private async Task<RateCard> DownloadAsync(Uri uri)
    {
        await _downloads.WaitAsync();
        try
        {
            using var response = await _http.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var document = await response.Content.ReadFromJsonAsync<RateCardDocument>()
                ?? throw new InvalidDataException($"Empty rate card at {uri}.");
            return document.ToRateCard(_clock.GetUtcNow());
        }
        finally
        {
            _downloads.Release();
        }
    }

    private bool IsStale(RateCard card) => _clock.GetUtcNow() - card.FetchedAt > TimeSpan.FromMinutes(30);

    public void Put(RateCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        lock (_gate)
        {
            _cards[card.Carrier] = card;
        }
    }

    public int EvictStale()
    {
        var evicted = 0;
        lock (_gate)
        {
            foreach (var key in _cards.Keys)
            {
                if (IsStale(_cards[key]))
                {
                    _cards.Remove(key);
                    evicted++;
                }
            }
        }

        return evicted;
    }

    public void Dispose() => _downloads.Dispose();

    private sealed record RateCardDocument(string Carrier, string Currency, decimal BaseFee, Dictionary<int, decimal> PerKgByZone, int VolumetricDivisor)
    {
        public RateCard ToRateCard(DateTimeOffset fetchedAt) =>
            new(Carrier, Currency, BaseFee, PerKgByZone, VolumetricDivisor, fetchedAt);
    }
}
