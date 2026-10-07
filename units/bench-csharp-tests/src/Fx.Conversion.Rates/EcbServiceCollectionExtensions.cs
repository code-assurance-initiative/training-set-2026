using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fx.Conversion.Rates.Ecb;

public static class EcbServiceCollectionExtensions
{
    /// <summary>The service key of the source behind the cache (the feed itself, or a fixed table in development).</summary>
    public const string OriginKey = "rates-origin";

    /// <summary>
    /// Registers the ECB feed behind a <see cref="RateCache"/> (served as <see cref="IRateSource"/>) and the background
    /// refresh that keeps it warm.
    /// </summary>
    public static IServiceCollection AddEcbRates(this IServiceCollection services)
    {
        services.AddHttpClient(EcbRateSource.HttpClientName, client => client.DefaultRequestHeaders.UserAgent.ParseAdd("fx-conversion/1.1"));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(sp => EcbResilience.Create(sp.GetRequiredService<TimeProvider>()));
        return services.AddRateCache<EcbRateSource>();
    }

    /// <summary>Puts <typeparamref name="TOrigin"/> behind the shared cache and its background refresh.</summary>
    public static IServiceCollection AddRateCache<TOrigin>(this IServiceCollection services)
        where TOrigin : class, IRateSource
    {
        services.AddOptions<EcbOptions>().BindConfiguration(EcbOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddKeyedSingleton<IRateSource, TOrigin>(OriginKey);
        services.AddSingleton(sp => new RateCache(
            sp.GetRequiredKeyedService<IRateSource>(OriginKey),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<IOptions<EcbOptions>>().Value.CacheTimeToLive,
            sp.GetRequiredService<ILogger<RateCache>>()));
        services.AddSingleton<IRateSource>(sp => sp.GetRequiredService<RateCache>());
        services.AddHostedService<RateRefreshService>();
        return services;
    }
}
