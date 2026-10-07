using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Shipping.Rates.Core.Carriers;
using Shipping.Rates.Core.Pricing;

namespace Shipping.Rates.IntegrationTests;

/// <summary>
/// Hosts the real API in memory. Replaced: the clock, the issuer metadata (so test tokens validate offline), the
/// carriers' HTTP endpoints (canned responses) and the rate cards (a fixed tariff instead of a download).
/// </summary>
public sealed class ShippingApiFactory : WebApplicationFactory<Program>
{
    private const string AlderResponses = """
        {"rates":[{"service":"standard","amount":5.10,"currency":"EUR","transitDays":2}],
         "trackingNumber":"A1234567890123","labelZpl":"^XA^XZ"}
        """;

    private const string CorvidResponses = """{"rates":[{"service":"STD","amount":4.90,"currency":"EUR","transit":"3"}]}""";

    private readonly string _labels = Directory.CreateTempSubdirectory("shipping-labels-").FullName;

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 3, 10, 8, 0, 0, TimeSpan.Zero));

    public TestTokens Tokens { get; } = new();

    public byte[] WebhookSecret { get; } = RandomNumberGenerator.GetBytes(32);

    public HttpClient CreateClient(params string[] scopes)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://rates.test") });
        if (scopes.Length > 0)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", Tokens.Create(scopes));
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Authentication:Authority", TestTokens.Issuer);
        builder.UseSetting("Authentication:Audience", TestTokens.Audience);
        builder.UseSetting("Webhooks:Secret", Convert.ToBase64String(WebhookSecret));
        builder.UseSetting("Labels:StorageRoot", _labels);
        builder.UseSetting("https_port", "443");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.RemoveAll<IRateCardProvider>();
            services.AddSingleton<IRateCardProvider, FixedRateCards>();
            services.AddHttpClient<AlderParcelAdapter>().ConfigurePrimaryHttpMessageHandler(() => new CannedHandler(AlderResponses));
            services.AddHttpClient<CorvidCourierAdapter>().ConfigurePrimaryHttpMessageHandler(() => new CannedHandler(CorvidResponses));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var metadata = Tokens.Metadata();
                options.Configuration = metadata;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Tokens.Dispose();
            Directory.Delete(_labels, recursive: true);
        }

        base.Dispose(disposing);
    }

    private sealed class FixedRateCards : IRateCardProvider
    {
        public RateCard GetCard(string carrier) => new(
            carrier,
            "EUR",
            4.00m,
            new Dictionary<int, decimal> { [1] = 1.00m, [2] = 1.50m, [3] = 2.00m, [4] = 3.00m, [5] = 6.00m },
            5000,
            DateTimeOffset.UnixEpoch);
    }

    private sealed class CannedHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
    }
}
