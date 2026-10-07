using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using ParcelTracking.Core.Carriers;
using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Tracking;
using ParcelTracking.Infrastructure.Carriers;
using ParcelTracking.Infrastructure.Health;
using ParcelTracking.Infrastructure.Notifications;
using ParcelTracking.Infrastructure.Persistence;

namespace ParcelTracking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTrackingPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Tracking")
            ?? throw new InvalidOperationException("ConnectionStrings:Tracking is not configured.");

        services.AddDbContext<TrackingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IParcelStore, ParcelStore>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(CarrierStatusMap.Default);
        services.AddScoped<TrackingService>();
        services.AddHealthChecks().AddDbContextCheck<TrackingDbContext>("database", tags: ["ready"]);
        return services;
    }

    public static IServiceCollection AddCarrierApi(this IServiceCollection services)
    {
        services.AddOptions<CarrierApiOptions>()
            .BindConfiguration(CarrierApiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var resilience = services.AddHttpClient<ICarrierClient, CarrierApiClient>(ConfigureCarrierClient)
            .AddStandardResilienceHandler();
        services.AddOptions<HttpStandardResilienceOptions>(resilience.PipelineName)
            .Configure<IOptions<CarrierApiOptions>>((options, carrier) =>
            {
                options.AttemptTimeout.Timeout = carrier.Value.AttemptTimeout;
                options.TotalRequestTimeout.Timeout = carrier.Value.TotalTimeout;
                options.Retry.MaxRetryAttempts = carrier.Value.MaxRetryAttempts;
            });

        // The readiness probe gets its own client: one short attempt and no retries, so the probe reports what the
        // carrier API is doing now rather than what a retry policy eventually achieved.
        services.AddHttpClient(CarrierApiHealthCheck.HttpClientName, (provider, client) =>
        {
            ConfigureCarrierClient(provider, client);
            client.Timeout = TimeSpan.FromSeconds(2);
        });

        services.AddSingleton<CarrierDirectory>();
        services.AddHealthChecks().AddCheck<CarrierApiHealthCheck>("carrier-api", tags: ["ready"]);
        return services;
    }

    public static IServiceCollection AddMerchantWebhooks(this IServiceCollection services)
    {
        services.AddOptions<WebhookOptions>()
            .BindConfiguration(WebhookOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IMerchantNotifier, MerchantWebhookClient>((provider, client) =>
            client.BaseAddress = provider.GetRequiredService<IOptions<WebhookOptions>>().Value.BaseAddress);

        return services;
    }

    private static void ConfigureCarrierClient(IServiceProvider provider, HttpClient client)
    {
        var carrier = provider.GetRequiredService<IOptions<CarrierApiOptions>>().Value;
        client.BaseAddress = carrier.BaseAddress;
        client.DefaultRequestHeaders.Add("X-Api-Key", carrier.ApiKey);
    }
}
