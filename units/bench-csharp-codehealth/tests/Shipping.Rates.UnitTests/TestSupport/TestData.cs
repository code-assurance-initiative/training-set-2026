using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shipping.Rates.Core.Domain;
using Shipping.Rates.Core.Pricing;

namespace Shipping.Rates.UnitTests.TestSupport;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 3, 10, 9, 30, 0, TimeSpan.Zero);

    public static Address Berlin => new("Ada Shop", "Ada GmbH", "Hauptstr. 1", "10115", "Berlin", "DE");

    public static Address Paris => new("Marie Client", null, "1 Rue de Rivoli", "75001", "Paris", "FR");

    public static Address Oslo => new("Ola Nordmann", null, "Karl Johans gate 1", "0154", "Oslo", "NO");

    public static Parcel Small => new(1_200, 30, 20, 10);

    public static Parcel Oversize => new(9_000, 130, 40, 30);

    public static RateCard Card(string carrier) => new(
        carrier,
        "EUR",
        4.00m,
        new Dictionary<int, decimal> { [1] = 1.00m, [2] = 1.50m, [3] = 2.00m, [4] = 3.00m, [5] = 6.00m },
        5000,
        Now);

    public static SurchargePolicy Policy(decimal markup = 1.0m) =>
        new(Options.Create(new SurchargeOptions { Markup = markup }), SurchargePolicy.PublishedTable);

    public static RateCalculator Calculator() => new(new FixedRateCards(), Policy(), new RemoteAreaLookup());
}

internal sealed class FixedRateCards : IRateCardProvider
{
    public RateCard GetCard(string carrier) => TestData.Card(carrier);
}

/// <summary>Answers every request with one canned response and records what was sent.</summary>
internal sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public List<(HttpMethod Method, string Path, string? Body)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sent = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request.Method, request.RequestUri?.AbsolutePath ?? string.Empty, sent));
        return new HttpResponseMessage(status) { Content = new StringContent(body) };
    }

    public HttpClient Client() => new(this) { BaseAddress = new Uri("https://carrier.test/") };
}

internal static class Loggers
{
    public static NullLogger<T> For<T>() => NullLogger<T>.Instance;
}
