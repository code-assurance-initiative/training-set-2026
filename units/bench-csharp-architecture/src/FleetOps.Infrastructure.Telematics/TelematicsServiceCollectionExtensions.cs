using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FleetOps.Infrastructure.Telematics;

public static class TelematicsServiceCollectionExtensions
{
    public static IServiceCollection AddTelematics(this IServiceCollection services)
    {
        services
            .AddOptions<TelematicsOptions>()
            .BindConfiguration(TelematicsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ITelematicsClient, TelematicsClient>((provider, http) =>
        {
            var options = provider.GetRequiredService<IOptions<TelematicsOptions>>().Value;
            http.BaseAddress = options.BaseAddress;
            http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        return services;
    }
}
