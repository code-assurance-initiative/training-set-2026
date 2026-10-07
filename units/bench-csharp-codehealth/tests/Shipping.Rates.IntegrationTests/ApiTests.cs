using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shipping.Rates.IntegrationTests;

public sealed class ApiTests(ShippingApiFactory factory) : IClassFixture<ShippingApiFactory>
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static object Address(string name, string postalCode, string city, string country) =>
        new { name, street = "Main street 1", postalCode, city, countryCode = country };

    private static object QuoteBody => new
    {
        sender = Address("Ada Shop", "10115", "Berlin", "DE"),
        recipient = Address("Marie Client", "75001", "Paris", "FR"),
        parcels = new[] { new { weightGrams = 1200, lengthCm = 30, widthCm = 20, heightCm = 10 } },
        level = "Standard",
    };

    [Fact]
    public async Task Health_is_public_and_carries_the_security_headers()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("max-age=31536000", response.Headers.GetValues("Strict-Transport-Security").Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Quotes_need_a_token_with_the_rates_scope()
    {
        using var anonymous = factory.CreateClient();
        using var wrongScope = factory.CreateClient("labels.write");

        using var unauthenticated = await anonymous.PostAsJsonAsync("/api/quotes", QuoteBody, Token);
        using var forbidden = await wrongScope.PostAsJsonAsync("/api/quotes", QuoteBody, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Quotes_are_priced_from_the_tariff_cheapest_first()
    {
        using var client = factory.CreateClient("rates.read");

        using var response = await client.PostAsJsonAsync("/api/quotes", QuoteBody, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token));
        var quotes = json.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, quotes.Count);
        Assert.Equal("ALDER", quotes[0].GetProperty("carrier").GetString());
        Assert.Equal(7.00m, quotes[0].GetProperty("price").GetDecimal());
        Assert.Equal("Corvid Courier", quotes[1].GetProperty("carrierName").GetString());
    }

    [Fact]
    public async Task An_invalid_quote_request_is_a_validation_problem()
    {
        using var client = factory.CreateClient("rates.read");

        using var response = await client.PostAsJsonAsync("/api/quotes", new { sender = Address("", "1", "x", "Germany") }, Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(Token);
        Assert.Contains("recipient", body, StringComparison.Ordinal);
        Assert.Contains("parcels", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_label_is_created_and_can_be_downloaded()
    {
        using var client = factory.CreateClient("labels.write");
        var request = new
        {
            carrier = "alder",
            service = "standard",
            sender = new { name = "Ada Shop", street = "Hauptstr. 1", postalCode = "10115", city = "Berlin", country = "DE" },
            recipient = new { name = "Marie Client", street = "1 Rue de Rivoli", postalCode = "75001", city = "Paris", country = "FR" },
            parcels = new[] { new { weightGrams = 1200, lengthCm = 30, widthCm = 20, heightCm = 10 } },
        };

        using var created = await client.PostAsJsonAsync("/api/labels", request, Token);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var downloaded = await client.GetAsync(created.Headers.Location, Token);

        Assert.Equal(HttpStatusCode.OK, downloaded.StatusCode);
        Assert.StartsWith("^XA", await downloaded.Content.ReadAsStringAsync(Token), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_label_is_not_found()
    {
        using var client = factory.CreateClient("labels.write");

        using var response = await client.GetAsync(new Uri("/api/labels/SR000", UriKind.Relative), Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Webhooks_are_accepted_only_with_a_valid_signature()
    {
        using var client = factory.CreateClient();
        const string payload = """{"trackingNumber":"A1234567890123","status":"delivered"}""";
        var signature = "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(factory.WebhookSecret, Encoding.UTF8.GetBytes(payload)));

        using var unsigned = new StringContent(payload, Encoding.UTF8, "application/json");
        using var rejected = await client.PostAsync(new Uri("/api/webhooks/tracking", UriKind.Relative), unsigned, Token);
        using var signed = new StringContent(payload, Encoding.UTF8, "application/json");
        signed.Headers.Add("X-Carrier-Signature", signature);
        using var accepted = await client.PostAsync(new Uri("/api/webhooks/tracking", UriKind.Relative), signed, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
    }
}
