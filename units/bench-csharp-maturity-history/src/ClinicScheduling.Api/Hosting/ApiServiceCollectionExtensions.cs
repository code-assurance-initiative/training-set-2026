using System.Text.Json.Serialization;
using ClinicScheduling.Api.Security;
using ClinicScheduling.Application;
using ClinicScheduling.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ClinicScheduling.Api.Hosting;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingApi(this IServiceCollection services)
    {
        services.AddSchedulingApplication();
        services.AddSchedulingInfrastructure();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
        return services.AddSchedulingSecurity();
    }

    private static IServiceCollection AddSchedulingSecurity(this IServiceCollection services)
    {
        services
            .AddOptions<JwtAuthenticationOptions>()
            .BindConfiguration(JwtAuthenticationOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasHttpsAuthority(), "Authentication:Authority must be an absolute https URL.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().AddSchedulingPolicies();
        return services;
    }
}
