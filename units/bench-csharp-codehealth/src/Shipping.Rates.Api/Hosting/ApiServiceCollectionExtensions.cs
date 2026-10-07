using System.Text.Json.Serialization;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Shipping.Rates.Api.Security;
using Shipping.Rates.Core.Accounts;
using Shipping.Rates.Core.Carriers;
using Shipping.Rates.Core.Labels;
using Shipping.Rates.Core.Pricing;

namespace Shipping.Rates.Api.Hosting;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddShippingApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddHealthChecks();
        services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
        services.AddSingleton(TimeProvider.System);
        services.AddShippingCore();
        services.AddCarriers(configuration);
        return services.AddShippingSecurity();
    }

    private static IServiceCollection AddShippingCore(this IServiceCollection services)
    {
        services.AddOptions<SurchargeOptions>().BindConfiguration(SurchargeOptions.SectionName);
        services.AddOptions<ShippingOptions>().BindConfiguration(ShippingOptions.SectionName);
        services.AddOptions<LabelOptions>().BindConfiguration(LabelOptions.SectionName);
        services.AddOptions<WebhookOptions>().BindConfiguration(WebhookOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();

        services.AddSingleton(sp => new SurchargePolicy(sp.GetRequiredService<IOptions<SurchargeOptions>>(), SurchargePolicy.PublishedTable));
        services.AddSingleton<RemoteAreaLookup>();
        services.AddSingleton<CutoffCalendar>();
        services.AddScoped<RateCalculator>();
        services.AddScoped<MultiParcelQuoter>();
        services.AddScoped<QuoteService>();
        services.AddScoped<LabelService>();
        services.AddHostedService<LabelRetentionService>();
        services.AddSingleton(sp => new LabelArchive(
            sp.GetRequiredService<IOptions<LabelOptions>>().Value.StorageRoot,
            sp.GetRequiredService<ILogger<LabelArchive>>()));
        services.AddSingleton(sp =>
        {
            var webhooks = sp.GetRequiredService<IOptions<WebhookOptions>>().Value;
            return new CarrierAccountManager(webhooks.RequestsPerMinute, webhooks.AuditCapacity, Convert.FromBase64String(webhooks.Secret));
        });
        return services;
    }

    private static IServiceCollection AddCarriers(this IServiceCollection services, IConfiguration configuration)
    {
        var carriers = configuration.GetSection(CarrierOptions.SectionName).Get<CarrierOptions>() ?? new CarrierOptions();
        // Retries only for safe methods: a retried label POST could book a shipment twice.
        services.AddHttpClient<AlderParcelAdapter>(client => Configure(client, carriers.Alder))
            .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
        services.AddHttpClient<CorvidCourierAdapter>(client => Configure(client, carriers.Corvid))
            .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
        services.AddScoped<ICarrierAdapter>(sp => sp.GetRequiredService<AlderParcelAdapter>());
        services.AddScoped<ICarrierAdapter>(sp => sp.GetRequiredService<CorvidCourierAdapter>());
        services.AddScoped<CarrierRegistry>();

        services.AddHttpClient(nameof(RateCardCache)).AddStandardResilienceHandler();
        services.AddSingleton(sp => new RateCardCache(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(RateCardCache)),
            carriers.RateCards.Select(card => new RateCardSource(card.Key, card.Value)),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<RateCardCache>>()));
        services.AddSingleton<IRateCardProvider>(sp => sp.GetRequiredService<RateCardCache>());

        if (!string.IsNullOrWhiteSpace(carriers.CustomAdapter))
        {
            // Partner adapters are an experimental extension point of Shipping.Rates.Core. This host opts in on
            // purpose, because operations load certified partner adapters by type name from configuration.
#pragma warning disable SHIPRATES001
            services.AddScoped(sp => CarrierRegistry.CreateCustom(carriers.CustomAdapter, sp));
#pragma warning restore SHIPRATES001
        }

        return services;
    }

    private static void Configure(HttpClient client, CarrierEndpoint endpoint)
    {
        client.BaseAddress = endpoint.BaseAddress;
        if (!string.IsNullOrEmpty(endpoint.ApiKey))
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", endpoint.ApiKey);
        }
    }

    private static IServiceCollection AddShippingSecurity(this IServiceCollection services)
    {
        services
            .AddOptions<JwtAuthenticationOptions>()
            .BindConfiguration(JwtAuthenticationOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasHttpsAuthority(), "Authentication:Authority must be an absolute https URL.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().AddShippingPolicies();
        return services;
    }
}
