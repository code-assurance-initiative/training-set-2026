using Fx.Conversion.Api.Errors;
using Fx.Conversion.Api.Security;
using Fx.Conversion.Currencies;
using Fx.Conversion.Quotes;
using Fx.Conversion.Rates;
using Fx.Conversion.Rates.Ecb;
using Fx.Conversion.TestSupport;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Fx.Conversion.Api.Hosting;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddConversionApi(this IServiceCollection services, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        services.AddConversionCore();
        if (environment.IsDevelopment())
        {
            services.AddRateCache<FixedRateSource>();
        }
        else
        {
            services.AddEcbRates();
        }

        services.AddControllers();
        services.AddProblemDetails();
        services.AddExceptionHandler<ConversionExceptionHandler>();
        services.AddHealthChecks();
        services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
        return services.AddConversionSecurity();
    }

    private static IServiceCollection AddConversionCore(this IServiceCollection services)
    {
        services.AddMetrics();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICurrencyCatalog, CurrencyCatalog>();
        services.AddSingleton<IFeePolicy, TieredFeePolicy>();
        services.AddSingleton<IQuoteStore, InMemoryQuoteStore>();
        services.AddSingleton<IQuoteAudit, LoggingQuoteAudit>();
        services.AddSingleton<IConversionMetrics, ConversionMetrics>();
        services.AddSingleton<CurrencyConverter>();
        services.AddSingleton<QuoteService>();
        return services;
    }

    private static IServiceCollection AddConversionSecurity(this IServiceCollection services)
    {
        services
            .AddOptions<JwtAuthenticationOptions>()
            .BindConfiguration(JwtAuthenticationOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasHttpsAuthority(), "Authentication:Authority must be an absolute https URL.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().AddConversionPolicies();
        return services;
    }
}
