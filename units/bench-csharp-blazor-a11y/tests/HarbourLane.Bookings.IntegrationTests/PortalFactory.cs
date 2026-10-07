using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace HarbourLane.Bookings.IntegrationTests;

/// <summary>
/// Hosts the portal in memory over HTTPS. The identity provider's metadata is supplied in memory, so a challenge
/// redirects to its authorize endpoint without any network call.
/// </summary>
public sealed class PortalFactory : WebApplicationFactory<Program>
{
    internal const string AuthorizeEndpoint = "https://login.harbourlane.org/connect/authorize";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("AllowedHosts", "localhost");
        builder.UseSetting("Authentication:Oidc:Authority", "https://login.harbourlane.org");
        builder.UseSetting("Authentication:Oidc:ClientId", "room-booking-portal");
        builder.ConfigureTestServices(services =>
            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
                options.Configuration = new OpenIdConnectConfiguration
                {
                    Issuer = "https://login.harbourlane.org",
                    AuthorizationEndpoint = AuthorizeEndpoint,
                    TokenEndpoint = "https://login.harbourlane.org/connect/token",
                }));
    }

    public HttpClient CreatePortalClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
    });
}
