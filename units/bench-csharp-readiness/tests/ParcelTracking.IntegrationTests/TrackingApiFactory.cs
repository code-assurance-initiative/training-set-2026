using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ParcelTracking.Core.Carriers;
using ParcelTracking.Infrastructure.Health;
using ParcelTracking.Infrastructure.Persistence;

namespace ParcelTracking.IntegrationTests;

/// <summary>
/// Hosts the real API in memory over a throw-away SQLite database, with the carrier API and the token issuer
/// replaced by in-process stand-ins.
/// </summary>
public sealed class TrackingApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TestTokens Tokens { get; } = new();

    public FakeCarrierClient Carrier { get; } = new();

    public HttpClient CreateClient(string? merchantId, params string[] scopes)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://parcels.test") });
        if (merchantId is not null)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", Tokens.Create(merchantId, scopes));
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.UseSetting("Authentication:Authority", TestTokens.Issuer);
        builder.UseSetting("Authentication:Audience", TestTokens.Audience);
        builder.UseSetting("ConnectionStrings:Tracking", "Data Source=unused");
        builder.UseSetting("CarrierApi:BaseAddress", "https://carrier.test/");
        builder.UseSetting("CarrierApi:ApiKey", "integration-tests-carrier-key");
        builder.UseSetting("https_port", "443");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<TrackingDbContext>>();
            services.RemoveAll<DbContextOptions<TrackingDbContext>>();
            services.AddDbContext<TrackingDbContext>(options => options
                .UseSqlite(_connection)
                .ReplaceService<IModelCustomizer, SqliteModelCustomizer>());

            services.RemoveAll<ICarrierClient>();
            services.AddSingleton<ICarrierClient>(Carrier);
            services.AddHttpClient(CarrierApiHealthCheck.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new StubHandler());

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var metadata = Tokens.Metadata();
                options.Configuration = metadata;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TrackingDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Tokens.Dispose();
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
