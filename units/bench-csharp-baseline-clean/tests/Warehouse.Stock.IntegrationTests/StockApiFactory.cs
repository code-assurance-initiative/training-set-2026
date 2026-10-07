using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Warehouse.Stock.IntegrationTests;

/// <summary>
/// Hosts the real API in memory. Only two things are replaced: the clock (so expiry is deterministic) and the
/// issuer metadata (so tokens from <see cref="TestTokens"/> validate without a network call).
/// </summary>
public sealed class StockApiFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 3, 2, 8, 0, 0, TimeSpan.Zero));

    public TestTokens Tokens { get; } = new();

    /// <summary>A client over HTTPS (so HSTS and redirection behave as in production) with the given scopes.</summary>
    public HttpClient CreateClient(params string[] scopes)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://stock.test") });
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
        builder.UseSetting("https_port", "443");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
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
        }

        base.Dispose(disposing);
    }
}
