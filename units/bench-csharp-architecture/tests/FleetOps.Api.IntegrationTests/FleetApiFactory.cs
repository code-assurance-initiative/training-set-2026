using FleetOps.Contracts.Vehicles;
using FleetOps.Infrastructure.Telematics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FleetOps.Api.IntegrationTests;

/// <summary>
/// Hosts the real API in memory over a private shared-cache SQLite database. Only the telematics vendor and the
/// issuer metadata are replaced.
/// </summary>
public sealed class FleetApiFactory : WebApplicationFactory<Program>
{
    private readonly string _database = $"file:fleet-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAlive;

    public FleetApiFactory()
    {
        _keepAlive = new SqliteConnection($"Data Source={_database}");
        _keepAlive.Open();
    }

    public TestTokens Tokens { get; } = new();

    public StubTelematics Telematics { get; } = new();

    public HttpClient CreateClient(params string[] scopes)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://fleet.test") });
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
        builder.UseSetting("Database:ConnectionString", $"Data Source={_database}");
        builder.UseSetting("https_port", "443");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITelematicsClient>();
            services.AddSingleton<ITelematicsClient>(Telematics);
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
            _keepAlive.Dispose();
        }

        base.Dispose(disposing);
    }

    public sealed class StubTelematics : ITelematicsClient
    {
        public int Odometer { get; set; }

        public Task<OdometerReading> GetOdometerAsync(string vin, CancellationToken cancellationToken) =>
            Task.FromResult(new OdometerReading(vin, Odometer, DateTimeOffset.UtcNow));

        public Task<VehiclePosition?> GetPositionAsync(string vin, CancellationToken cancellationToken) =>
            Task.FromResult<VehiclePosition?>(null);

        public Task<bool> PingAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
