using Microsoft.AspNetCore.Authentication.JwtBearer;
using Warehouse.Stock.Api.Security;
using Warehouse.Stock.Application;
using Warehouse.Stock.Infrastructure;

namespace Warehouse.Stock.Api.Hosting;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddStockApi(this IServiceCollection services)
    {
        services.AddStockApplication();
        services.AddStockInfrastructure();
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
        return services.AddStockSecurity();
    }

    private static IServiceCollection AddStockSecurity(this IServiceCollection services)
    {
        services
            .AddOptions<JwtAuthenticationOptions>()
            .BindConfiguration(JwtAuthenticationOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasHttpsAuthority(), "Authentication:Authority must be an absolute https URL.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().AddStockPolicies();
        return services;
    }
}
