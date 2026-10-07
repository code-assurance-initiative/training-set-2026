using FleetOps.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FleetOps.ServiceDefaults;

/// <summary>What every FleetOps host gets: a clock, structured console logging and the health checks.</summary>
public static class ServiceDefaultsExtensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
        builder.Services.AddHealthChecks().AddFleetDiagnostics();
        return builder;
    }
}
