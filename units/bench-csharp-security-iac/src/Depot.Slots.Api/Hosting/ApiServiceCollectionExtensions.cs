using System.Net;
using Depot.Slots.Api.Security;
using Depot.Slots.Core.Bookings;
using Depot.Slots.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;

namespace Depot.Slots.Api.Hosting;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddSlotsApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddSingleton(TimeProvider.System);
        services
            .AddOptions<DockOptions>()
            .BindConfiguration(DockOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<BookingService>();
        services.AddBookingStorage(configuration, ownsSchema: true);

        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
        services.Configure<ForwardedHeadersOptions>(options => ConfigureForwardedHeaders(options, configuration));
        return services.AddSlotsSecurity();
    }

    /// <summary>
    /// TLS ends at the ingress controller; only its pod network is trusted to say what the original scheme and
    /// client address were.
    /// </summary>
    private static void ConfigureForwardedHeaders(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        var trusted = configuration["ForwardedHeaders:TrustedNetwork"];
        if (!string.IsNullOrEmpty(trusted))
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(trusted));
        }

        options.KnownProxies.Add(IPAddress.Loopback);
    }

    private static IServiceCollection AddSlotsSecurity(this IServiceCollection services)
    {
        services
            .AddOptions<JwtAuthenticationOptions>()
            .BindConfiguration(JwtAuthenticationOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasHttpsAuthority(), "Authentication:Authority must be an absolute https URL.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().AddSlotsPolicies();
        return services;
    }
}
